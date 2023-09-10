using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Linq;
using System.Text;

namespace WeirdStrings
{
    internal class Program
    {
        private static ModuleDefMD Module = null;
        private static MethodDef GetStringMethod = null;

        static void Main(string[] args)
        {
            Module = ModuleDefMD.Load(args[0]);

            Strings(Module); //Processing Strings
            Watermark(Module); //Adding Aid Watermark

            Module.Write(Module.Location.Insert(Module.Location.Length - 4, "-WeirdStrings"));
        }

        private static void Strings(ModuleDefMD module)
        {
            //Runtime Type
            Type MyType = typeof(Runtime);

            //Runtime TypeDef using DNLib
            TypeDef Runtime = ModuleDefMD.Load(MyType.Module).ResolveTypeDef(MDToken.ToRID(MyType.MetadataToken));

            GetStringMethod = Runtime.Methods.First(M => M.Name == "TheHellTower");

            //We need to remove it from Runtime to avoid a reference still existing problem
            Runtime.Methods.Remove(GetStringMethod);

            //Obviously we need to be able to reach it..
            Module.GlobalType.Methods.Add(GetStringMethod);

            GetStringMethod.Name = "WhatTheFuck"; //New Method Name
            GetStringMethod.DeclaringType.Name = "https://github.com/TheHellTower"; //New GlobalType Name

            foreach(TypeDef Type in Module.GetTypes().Where(T => T.HasMethods)) //"GetTypes()" so we also actually can access nested types.
                foreach (MethodDef Method in Type.Methods.Where(M => !M.IsConstructor && M.HasBody && M.Body.HasInstructions)) //We don't want to deal with constructors to void needing to rename types/class and we only take methods that has a body with instructions
                {
                    if (Method == GetStringMethod) continue; //We ignore our own Runtime method
                    Console.WriteLine($"Processing Method: {Method.Name}");

                    string NewMethodName = ""; //This will be used for our dumb trick !

                    for(int I = 0; I < Method.Body.Instructions.Count(); I++)
                    {
                        Instruction Instruction = Method.Body.Instructions[I]; //Doing it like this so easier to deal with the instruction + can get pos of this instruction too if it's the one we are looking for.
                        if(Instruction.OpCode == OpCodes.Ldstr) //We only aim at strings there.
                        {
                            string MyEncodedString = Convert.ToBase64String(Encoding.UTF8.GetBytes(Instruction.Operand.ToString())).Replace("=", "*");

                            string originalMethodName = NewMethodName; //Shhht don't worry
                            string modifiedMethodName = originalMethodName + MyEncodedString;

                            int X = originalMethodName.Length;
                            int Y = modifiedMethodName.Length;

                            Console.WriteLine($"Processing String: \"{MyEncodedString}\", X={X} & Y={Y}");

                            NewMethodName += MyEncodedString; //Remember, X & Y are positions !
                            originalMethodName += MyEncodedString; //Obviously to make it work..

                            Instruction.OpCode = OpCodes.Call;
                            Instruction.Operand = GetStringMethod; //String replaced by our Method

                            Method.Body.Instructions.Insert(I, new Instruction(OpCodes.Ldc_I4, Y));
                            Method.Body.Instructions.Insert(I, new Instruction(OpCodes.Ldc_I4, X));
                        }
                    }

                    if (NewMethodName != "")
                        Method.Name = NewMethodName;

                    Method.Body.OptimizeBranches();
                    Method.Body.SimplifyBranches();
                    Method.Body.OptimizeMacros();
                }
        }

        //Not really the important part so will not comment
        private static void Watermark(ModuleDefMD Module)
        {
            var attrName = "WeirdStringsBy";
            var attrRef = Module.CorLibTypes.GetTypeRef("System", "Attribute");
            var attrType = Module.FindNormal(attrName); //Just in case the assembly already got processed. Even thought if it did no strings should left so that would be dumb to pass it again there.
            if(attrType == null)
            {
                attrType = new TypeDefUser("", attrName, attrRef);
                Module.Types.Add(attrType);
            }

            var ctor = attrType.FindInstanceConstructors().FirstOrDefault(M => M.Parameters.Count() == 1 && M.Parameters[0].Type == Module.CorLibTypes.String);
            if(ctor == null)
            {
                //Aid enough(jk)
                ctor = new MethodDefUser("WeirdStrings", MethodSig.CreateInstance(Module.CorLibTypes.Void, Module.CorLibTypes.String), MethodImplAttributes.Managed, MethodAttributes.HideBySig | MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)
                {
                    Body = new CilBody { MaxStack = 1 }
                };

                ctor.Body.Instructions.Add(OpCodes.Ldstr.ToInstruction(GetStringMethod.DeclaringType.Name));
                ctor.Body.Instructions.Add(OpCodes.Newobj.ToInstruction(new MemberRefUser(Module, ".ctor", MethodSig.CreateInstance(Module.CorLibTypes.Void, Module.CorLibTypes.String), Module.CorLibTypes.GetTypeRef("System", "Exception"))));
                ctor.Body.Instructions.Add(OpCodes.Throw.ToInstruction());

                attrType.Methods.Add(ctor);
            }

            var attr = new CustomAttribute(ctor);
            attr.ConstructorArguments.Add(new CAArgument(Module.CorLibTypes.String, "TheHellTower"));

            Module.CustomAttributes.Add(attr);

            foreach(TypeDef Type in Module.GetTypes().Where(T => T.HasMethods))
            {
                Type.CustomAttributes.Add(attr);
                foreach(MethodDef Method in Type.Methods.Where(M => M.HasBody && M.Body.HasInstructions && M.Body.Instructions.Count() > 1))
                    Method.CustomAttributes.Add(attr);
                foreach(FieldDef Field in Type.Fields.Where(F => !F.DeclaringType.IsEnum && !F.DeclaringType.IsForwarder && !F.DeclaringType.IsRuntimeSpecialName))
                    Field.CustomAttributes.Add(attr);
                foreach (EventDef Event in Type.Events.Where(E => !E.DeclaringType.IsForwarder && !E.IsRuntimeSpecialName))
                    Event.CustomAttributes.Add(attr);
            }
        }
    }
}