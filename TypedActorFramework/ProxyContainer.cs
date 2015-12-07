using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using ActorInterface;
using TypedActorInterface;

namespace TypedActorFramework
{
    public class ProxyContainer
    {
        private Dictionary<Type, Type> proxyTypes;
        private readonly AssemblyName aName;
        private readonly AssemblyBuilder ab;
        private readonly ModuleBuilder mb;

        public ProxyContainer()
        {
            proxyTypes = new Dictionary<Type, Type>();

            aName = new AssemblyName("DynamicProxyAssembly");

            ab =
                AppDomain.CurrentDomain.DefineDynamicAssembly(
                    aName,
                    AssemblyBuilderAccess.RunAndSave);

            mb = ab.DefineDynamicModule(
                aName.Name,
                aName.Name + ".dll");
        }

        /// <summary>
        /// For debugging.
        /// </summary>
        public void SaveModule()
        {
            ab.Save(aName.Name + ".dll");
        }

        public Type GetProxyType(Type actorType)
        {
            Type res;
            proxyTypes.TryGetValue(actorType, out res);
            if (res == null)
            {
                res = CreateProxyType(mb, actorType);
                proxyTypes.Add(actorType, res);

                //ab.Save(aName.Name + ".dll");
            }
            return res;
        }

        private static Type CreateProxyType(ModuleBuilder mb, Type actorType)
        {
            if (!actorType.IsInterface)
            {
                throw new InvalidOperationException();
            }

            // public auto ansi beforefieldinit
            TypeBuilder tb = mb.DefineType(
                actorType.FullName + "$Proxy",
                TypeAttributes.Public | TypeAttributes.BeforeFieldInit);

            tb.AddInterfaceImplementation(actorType);

            foreach (var i in actorType.GetInterfaces())
            {
                tb.AddInterfaceImplementation(i);
            }

            // Fields

            var fieldActorMailbox = tb.DefineField("mailbox",
                typeof(IMailbox<object>),
                FieldAttributes.Private);

            var fieldRuntime = tb.DefineField("runtime",
                typeof(IActorRuntime),
                FieldAttributes.Private);

            // Constructor

            // public hidebysig specialname rtspecialname
            var constructorBuilder = tb.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig,
                CallingConventions.Standard,
                new Type[] {fieldActorMailbox.FieldType, fieldRuntime.FieldType});

            var constrILGen = constructorBuilder.GetILGenerator();

            constrILGen.Ldarg(0);
            constrILGen.Emit(OpCodes.Call,
                typeof(object).GetConstructor(Type.EmptyTypes));
            constrILGen.Ldarg(0);
            constrILGen.Ldarg(1);
            constrILGen.Emit(OpCodes.Stfld, fieldActorMailbox);
            constrILGen.Ldarg(0);
            constrILGen.Ldarg(2);
            constrILGen.Emit(OpCodes.Stfld, fieldRuntime);
            constrILGen.Emit(OpCodes.Ret);


            // Methods

            var methods = actorType.GetMethods().ToList();

            foreach (var i in actorType.GetInterfaces())
            {
                methods.AddRange(i.GetMethods());
            }

            foreach (var m in methods)
            {
                List<FieldBuilder> methodParamFields = new List<FieldBuilder>();
                FieldBuilder resultField = null;
                FieldBuilder resultMailboxField = null;
                FieldBuilder exceptionField = null;

                Type messageClassType = CreateMessageClass(mb,
                    actorType,
                    m,
                    methodParamFields,
                    ref resultField,
                    ref resultMailboxField,
                    ref exceptionField);

                CreateMethodBody(tb,
                    m,
                    messageClassType,
                    fieldActorMailbox,
                    fieldRuntime,
                    methodParamFields,
                    resultField,
                    resultMailboxField,
                    exceptionField);
            }

            Type proxyType = tb.CreateType();

            // Done
            return proxyType;
        }

        private static Type CreateMessageClass(ModuleBuilder mb, Type actorType, MethodInfo m, List<FieldBuilder> methodParamFields, ref FieldBuilder resultField, ref FieldBuilder resultMailboxField, ref FieldBuilder exceptionField)
        {
            Safety.Assert(m.DeclaringType != null);

            Type returnType = null;
            Type mailboxResultType = null;


            TypeBuilder tb = mb.DefineType(
                $"{m.DeclaringType.FullName}${m.Name}$Message",
                TypeAttributes.Public | TypeAttributes.BeforeFieldInit);

            tb.AddInterfaceImplementation(typeof(ICallable));

            if (m.ReturnType != typeof(void))
            {
                returnType = m.ReturnType;
                mailboxResultType = typeof(IMailbox<object>);
            }

            List<FieldBuilder> constructorFields = new List<FieldBuilder>();

            

            foreach (var p in m.GetParameters())
            {
                Type fieldType = p.ParameterType;

                methodParamFields.Add(
                    tb.DefineField(
                        p.Name,
                        fieldType.IsByRef ? fieldType.GetElementType() : fieldType,
                        FieldAttributes.Public));
            }

            constructorFields.AddRange(methodParamFields);

            if (returnType != null)
            {
                resultField =
                    tb.DefineField("$result",
                        returnType,
                        FieldAttributes.Public);
                resultMailboxField = tb.DefineField("$resultMailbox",
                    mailboxResultType,
                    FieldAttributes.Public);
                exceptionField = tb.DefineField("$exception",
                    typeof (Exception),
                    FieldAttributes.Public);

                constructorFields.Add(resultMailboxField);
            }

            
            // Constructor

            ConstructorBuilder ctor = tb.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig,
                CallingConventions.Standard,
                constructorFields.Select(builder => builder.FieldType).ToArray());

            ILGenerator ctorIL = ctor.GetILGenerator();

            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call,
                typeof(object).GetConstructor(Type.EmptyTypes));

            const int firstParamPos = 1; // excluding "this" (0) 

            for (int i = 0; i < constructorFields.Count; ++i)
            {
                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Ldarg(firstParamPos + i);
                ctorIL.Emit(OpCodes.Stfld, constructorFields[i]);
            }
            ctorIL.Emit(OpCodes.Ret);

            // Method "Call"
            // public final hidebysig newslot virtual
            var methodBuilder = tb.DefineMethod(nameof(ICallable.Call),
                MethodAttributes.Public | MethodAttributes.Final |
                MethodAttributes.HideBySig | MethodAttributes.NewSlot |
                MethodAttributes.Virtual,
                CallingConventions.Standard,
                typeof(void),
                new Type[] { typeof(ITypedActor)/*, typeof(IActorRuntimeInternal)*/ }
                );

            var callIL = methodBuilder.GetILGenerator();

            LocalBuilder exceptionLocal = null;

            Label endOfMethod = default(Label);

            if (returnType != null)
            {
                exceptionLocal = callIL.DeclareLocal(exceptionField.FieldType);
                endOfMethod = callIL.DefineLabel();

                callIL.BeginExceptionBlock();
                callIL.Ldarg(0);
            }

            callIL.Ldarg(1);
            callIL.Emit(OpCodes.Castclass, actorType);

            for(int i = 0; i < methodParamFields.Count; ++i)
            {
                callIL.Ldarg(0);
                // [this message object, other params ... , actor instance]
                callIL.Emit(
                    m.GetParameters()[i].ParameterType.IsByRef
                        ? OpCodes.Ldflda
                        : OpCodes.Ldfld,
                    methodParamFields[i]);
                // [field value, other params ... , actor instance]
            }

            // [params, actor instance]
            callIL.Emit(OpCodes.Callvirt, m);

            if (returnType == null)
            {
                // []
                callIL.Emit(OpCodes.Ret);
            }
            else
            {
                /////// callIL.Emit(OpCodes.);

                callIL.Emit(OpCodes.Stfld, resultField);
                callIL.Ldarg(0);
                callIL.Emit(OpCodes.Ldfld, resultMailboxField);
                callIL.Ldarg(0);
                callIL.Emit(OpCodes.Callvirt,
                    mailboxResultType.GetMethod(nameof(IMailbox<object>.Send)));

                callIL.BeginCatchBlock(typeof(Exception));

                // [ ex ]
                callIL.Emit(OpCodes.Stloc, exceptionLocal);
                callIL.Ldarg(0);
                callIL.Emit(OpCodes.Ldloc, exceptionLocal);
                callIL.Emit(OpCodes.Stfld, exceptionField);
                callIL.Ldarg(0);
                callIL.Emit(OpCodes.Ldfld, resultMailboxField);
                callIL.Ldarg(0);
                callIL.Emit(OpCodes.Callvirt,
                    mailboxResultType.GetMethod(nameof(IMailbox<object>.Send)));

                callIL.EndExceptionBlock();

                callIL.MarkLabel(endOfMethod);
                // [ ]
                callIL.Emit(OpCodes.Ret);
            }


            // Method ToString
            // public hidebysig virtual instance
            methodBuilder = tb.DefineMethod(nameof(ToString),
                MethodAttributes.Public |
                MethodAttributes.HideBySig | MethodAttributes.Virtual,
                CallingConventions.Standard,
                typeof(string),
                Type.EmptyTypes
                );
            callIL = methodBuilder.GetILGenerator();

            callIL.Emit(OpCodes.Ldstr, m.Name);
            callIL.Emit(OpCodes.Ret);



            // Done
            Type t = tb.CreateType();

            return t;

        }

        private static void CreateMethodBody(
            TypeBuilder tb,
            MethodInfo mi,
            Type messageClassType,
            FieldBuilder fieldActorMailbox,
            FieldBuilder fieldRuntime,
            List<FieldBuilder> methodParamFields, FieldBuilder resultField, FieldBuilder resultMailboxField, FieldBuilder exceptionField)
        {
            Type[] paramTypes =
                mi.GetParameters().Select(p => p.ParameterType).ToArray();

            // public final hidebysig newslot virtual
            var methodBuilder = tb.DefineMethod(mi.Name,
                MethodAttributes.Public | MethodAttributes.Final |
                MethodAttributes.HideBySig | MethodAttributes.NewSlot |
                MethodAttributes.Virtual,
                CallingConventions.Standard,
                mi.ReturnType,
                paramTypes);

            Type returnType = null;
            Type mailboxType = null;

            if (mi.ReturnType != typeof(void))
            {
                returnType = mi.ReturnType;
                mailboxType = typeof(IMailbox<object>);
            }

            var ilGen = methodBuilder.GetILGenerator();


            var messageLocal = ilGen.DeclareLocal(messageClassType);
            if (returnType == null)
            {
                LoadArguments(paramTypes, ilGen);
                ilGen.Emit(OpCodes.Newobj,
                    messageClassType.GetConstructors().Single());
                // [ msg ]
                ilGen.Emit(OpCodes.Stloc, messageLocal);
                // [ ]
                ilGen.Ldarg(0);
                ilGen.Emit(OpCodes.Ldfld, fieldActorMailbox);
                // [ mailbox ]
                ilGen.Emit(OpCodes.Ldloc, messageLocal);
                // [ msg, mailbox ]
                ilGen.Emit(OpCodes.Callvirt,
                    typeof(IMailbox<object>).GetMethod(
                        nameof(IMailbox<object>.Send)));
                // []
                ilGen.Emit(OpCodes.Ret);
            }
            else
            {
                var mailboxLocal = ilGen.DeclareLocal(mailboxType);
                var exceptionLocal = ilGen.DeclareLocal(exceptionField.FieldType);

                var labelAfterThrow = ilGen.DefineLabel();

                ilGen.Ldarg(0);
                ilGen.Emit(OpCodes.Ldfld, fieldRuntime);
                ilGen.Emit(OpCodes.Callvirt,
                    typeof (IActorRuntime).GetMethod(
                        nameof(IActorRuntime.CreateMailbox))
                        .MakeGenericMethod(typeof(object)));
                ilGen.Emit(OpCodes.Stloc, mailboxLocal);
                LoadArguments(paramTypes, ilGen);
                ilGen.Emit(OpCodes.Ldloc, mailboxLocal);
                ilGen.Emit(OpCodes.Newobj,
                    messageClassType.GetConstructors().Single());

                ilGen.Emit(OpCodes.Stloc, messageLocal);
                ilGen.Ldarg(0);
                ilGen.Emit(OpCodes.Ldfld, fieldActorMailbox);
                ilGen.Emit(OpCodes.Ldloc, messageLocal);
                ilGen.Emit(OpCodes.Callvirt,
                    typeof(IMailbox<object>).GetMethod(
                        nameof(IMailbox<object>.Send)));

                ilGen.Emit(OpCodes.Ldloc, mailboxLocal);
                ilGen.Emit(OpCodes.Callvirt,
                    mailboxType.GetMethod(nameof(IMailbox<object>.Receive)));
                ilGen.Emit(OpCodes.Castclass, messageClassType);
                ilGen.Emit(OpCodes.Stloc, messageLocal);

                // set by ref params
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    if (paramTypes[i].IsByRef)
                    {
                        // load address of arg
                        ilGen.Ldarg(i+1);
                        ilGen.Emit(OpCodes.Ldloc, messageLocal);
                        ilGen.Emit(OpCodes.Ldfld, methodParamFields[i]);
                        ilGen.Emit(OpCodes.Stobj, methodParamFields[i].FieldType);
                    }
                }

                
                ilGen.Emit(OpCodes.Ldloc, messageLocal);
                ilGen.Emit(OpCodes.Ldfld, exceptionField);
                ilGen.Emit(OpCodes.Stloc, exceptionLocal);
                ilGen.Emit(OpCodes.Ldloc, exceptionLocal);
                ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Cgt_Un);
                ilGen.Emit(OpCodes.Brfalse_S, labelAfterThrow);

                ilGen.Emit(OpCodes.Ldloc, exceptionLocal);
                ilGen.Emit(OpCodes.Call,
                    typeof (ExceptionDispatchInfo).GetMethod(
                        nameof(ExceptionDispatchInfo.Capture)));
                ilGen.Emit(OpCodes.Callvirt,
                    typeof (ExceptionDispatchInfo).GetMethod(
                        nameof(ExceptionDispatchInfo.Throw)));
                

                ilGen.MarkLabel(labelAfterThrow);
                ilGen.Emit(OpCodes.Ldloc, messageLocal);
                ilGen.Emit(OpCodes.Ldfld, resultField);
                ilGen.Emit(OpCodes.Ret);
            }
        }

        private static void LoadArguments(
            Type[] paramTypes,
            ILGenerator ilGen)
        {
            for (int i = 0; i < paramTypes.Length; ++i)
            {
                ilGen.Ldarg(i + 1);
            }
            // [ params ... ]
            
        }
    }
}