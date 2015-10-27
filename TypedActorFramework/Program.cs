using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TypedActorInterface;

namespace TypedActorFramework
{
    class Program
    {
        static void Main(string[] args)
        {
            AssemblyName aName = new AssemblyName("DynamicAssemblyExample");

            AssemblyBuilder ab =
                AppDomain.CurrentDomain.DefineDynamicAssembly(
                    aName,
                    AssemblyBuilderAccess.RunAndSave);

            ModuleBuilder mb = ab.DefineDynamicModule(
                aName.Name,
                aName.Name + ".dll");


            Type actorType = null;
            if (!actorType.IsInterface)
            {
                throw new Exception();
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

            var fieldActorId = tb.DefineField("id",
                typeof(ActorId<>).MakeGenericType(actorType),
                FieldAttributes.Private);

            var fieldRuntime = tb.DefineField("runtime",
                typeof(IActorRuntimeInternal),
                FieldAttributes.Private);

            // Constructor

            // public hidebysig specialname rtspecialname
            var constructorBuilder = tb.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig,
                CallingConventions.Standard,
                new Type[] { fieldActorId.FieldType, fieldRuntime.FieldType });

            var constrILGen = constructorBuilder.GetILGenerator();

            constrILGen.Ldarg(0);
            constrILGen.Emit(OpCodes.Call,
                typeof(object).GetConstructor(Type.EmptyTypes));
            constrILGen.Ldarg(0);
            constrILGen.Ldarg(1);
            constrILGen.Emit(OpCodes.Stfld, fieldActorId);
            constrILGen.Ldarg(0);
            constrILGen.Ldarg(2);
            constrILGen.Emit(OpCodes.Stfld, fieldRuntime);
            constrILGen.Emit(OpCodes.Ret);


            // Methods

            var methods = actorType.GetMethods();

            foreach (var m in methods)
            {
                Type messageClassType = CreateMessageClass(mb, actorType, m);
                CreateMethodBody(tb, m, messageClassType, fieldRuntime);
            }

            Type proxyType = tb.CreateType();

            // Done

            ab.Save(aName.Name + ".dll");

        }

        private static Type CreateMessageClass(ModuleBuilder mb, Type actorType, MethodInfo m)
        {
            TypeBuilder tb = mb.DefineType(
                $"{actorType.FullName}${m.Name}$Message",
                TypeAttributes.Public | TypeAttributes.BeforeFieldInit);

            tb.AddInterfaceImplementation(typeof(ICallable));

            List<Type> paramTypes =
                m.GetParameters().Select(p => p.ParameterType).ToList();

            List<FieldBuilder> fields = new List<FieldBuilder>();

            foreach (var p in m.GetParameters())
            {
                Type fieldType = p.ParameterType;

                // If the field type (e.g. IXxx) is derived from an IActor, make it 
                // ActorId<IXxx> instead.
                if (fieldType.GetInterface(nameof(ITypedActor)) != null)
                {
                    fieldType =
                        typeof(ActorId<>).MakeGenericType(p.ParameterType);
                }

                fields.Add(
                    tb.DefineField(
                        p.Name,
                        fieldType,
                        FieldAttributes.Public));
            }

            List<Type> constructorParamTypes = new List<Type>();
            constructorParamTypes.Add(typeof(IActorRuntimeInternal));
            constructorParamTypes.AddRange(paramTypes);

            // Constructor


            ConstructorBuilder ctor = tb.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig,
                CallingConventions.Standard,
                constructorParamTypes.ToArray());

            ILGenerator ctorIL = ctor.GetILGenerator();

            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call,
                typeof(object).GetConstructor(Type.EmptyTypes));

            const int firstParamPos = 2; // excluding "this" (0) and "runtime" (1) param.

            for (int i = 0; i < fields.Count; ++i)
            {
                ctorIL.Emit(OpCodes.Ldarg_0);

                if (paramTypes[i] != fields[i].FieldType)
                {
                    ctorIL.Emit(OpCodes.Ldarg_1); // load runtime
                    ctorIL.Ldarg(firstParamPos + i);

                    var getActorIdMethod =
                        typeof(IActorRuntimeInternal).GetMethod(
                            nameof(IActorRuntimeInternal.GetActorId))
                            .MakeGenericMethod(paramTypes[i]);

                    ctorIL.Emit(OpCodes.Callvirt, getActorIdMethod);
                }
                else
                {
                    ctorIL.Ldarg(firstParamPos + i);
                }

                ctorIL.Emit(OpCodes.Stfld, fields[i]);
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
                new Type[] { typeof(ITypedActor), typeof(IActorRuntimeInternal) }
                );

            var callIL = methodBuilder.GetILGenerator();
            callIL.Ldarg(1);
            callIL.Emit(OpCodes.Castclass, actorType);

            for (int i = 0; i < fields.Count; ++i)
            {
                if (paramTypes[i] != fields[i].FieldType)
                {
                    var methodGetActorProxy =
                        typeof(IActorRuntimeInternal).GetMethod(
                            nameof(IActorRuntimeInternal.GetActorProxy))
                            .MakeGenericMethod(paramTypes[i]);

                    // [other params ... , actor instance]
                    callIL.Ldarg(2);
                    // [runtime, other params ... , actor instance]
                    callIL.Ldarg(0);
                    // [this message object, runtime, other params ... , actor instance]
                    callIL.Emit(OpCodes.Ldfld, fields[i]); // field that needs converting
                    // [field value, runtime, other params ... , actor instance]
                    callIL.Emit(OpCodes.Callvirt, methodGetActorProxy);
                    // [converted field value, other params ... , actor instance]
                }
                else
                {
                    // [other params ... , actor instance]
                    callIL.Ldarg(0);
                    // [this message object, other params ... , actor instance]
                    callIL.Emit(OpCodes.Ldfld, fields[i]);
                    // [field value, other params ... , actor instance]
                }
            }

            // [params, actor instance]
            callIL.Emit(OpCodes.Callvirt, m);
            // []
            callIL.Emit(OpCodes.Ret);


            // Done
            Type t = tb.CreateType();

            return t;

        }

        private static void CreateMethodBody(
            TypeBuilder tb,
            MethodInfo mi,
            Type messageClassType,
            FieldBuilder fieldRuntime)
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



            var ilGen = methodBuilder.GetILGenerator();

            ilGen.DeclareLocal(messageClassType);

            // []
            ilGen.Ldarg(0);
            // [ this ]
            ilGen.Emit(OpCodes.Ldfld, fieldRuntime);
            // [ runtime ]
            for (int i = 0; i < paramTypes.Length; ++i)
            {
                ilGen.Ldarg(i + 1);
            }
            // [ params ..., runtime ]
            ilGen.Emit(OpCodes.Newobj,
                messageClassType.GetConstructors().Single());
            // [ message object]
            ilGen.Emit(OpCodes.Stloc_0);
            // [ ]
            ilGen.Ldarg(0);
            // [ this ]
            ilGen.Emit(OpCodes.Ldfld, fieldRuntime);
            // [ runtime ]
            ilGen.Ldarg(0);
            // [ this, runtime ]
            ilGen.Emit(OpCodes.Ldloc_0);
            // [ message object, this, runtime ]
            ilGen.Emit(OpCodes.Callvirt,
                typeof(IActorRuntimeInternal).GetMethod(
                    nameof(IActorRuntimeInternal.Send)));
            // []
            ilGen.Emit(OpCodes.Ret);
        }


    }
}
