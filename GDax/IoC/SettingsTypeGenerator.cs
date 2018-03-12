using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.CSharp;
using GDax.Helpers;

namespace GDax.IoC
{
    internal static class SettingsTypeGenerator
    {
        private const string SettingsClassName = "TickerSettings";
        private const string PropertyChangedMethodName = "OnPropertyChanged";
        private const string PropertyChangedParameterName = "propertyName";
        private const string ReadXmlParameterName = "reader";
        private const string WriteXmlParameterName = "writer";
        private const string SettingsNameSpace = "GDax.Settings";
        private static Dictionary<Type, Type> _proxies;
        private static Type _settingsType;

        public static void GenerateSettingTypes()
        {
            var types = from t in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.StartsWith("GDax", StringComparison.Ordinal)).SelectMany(a => a.GetTypes())
                        where t.IsInterface && t.Namespace == SettingsNameSpace
                        select t;

            GenerateTypes(types.ToList());
        }

        public static Type GetApplicationSettingsType()
        {
            return _settingsType;
        }

        public static Type GetImplementationType(Type type)
        {
            if (!_proxies.ContainsKey(type))
                throw new InvalidOperationException($"Invalid setting type '{type.Name}'.");

            return _proxies[type];
        }

        private static void GenerateTypes(List<Type> types)
        {
            var codeNamespace = new CodeNamespace($"{SettingsNameSpace}.Internals_");

            var settingsClass = new CodeTypeDeclaration { Name = SettingsClassName, TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed };
            settingsClass.BaseTypes.Add(new CodeTypeReference(typeof(ApplicationSettingsBase)));
            settingsClass.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(SettingsProviderAttribute)), new CodeAttributeArgument(new CodeTypeOfExpression(new CodeTypeReference(typeof(GlobalFileSettingsProvider))))));

            foreach (var type in types)
                codeNamespace.Types.Add(CreateType(type, settingsClass));

            codeNamespace.Types.Add(settingsClass);

            var compiler = new CSharpCodeProvider();
            var parameters = new CompilerParameters { GenerateInMemory = true };

            var compileUnit = new CodeCompileUnit();
            compileUnit.ReferencedAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(a => a.Location).ToArray());
            compileUnit.Namespaces.Add(codeNamespace);

            TraceCodeGeneration(compiler, compileUnit);

            var result = compiler.CompileAssemblyFromDom(parameters, compileUnit);
            if (result.Errors.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (CompilerError error in result.Errors)
                    sb.AppendLine(error.ErrorText);
                throw new InvalidOperationException($"Compiler errors:{Environment.NewLine}{sb}");
            }


            var generatedTypes = result.CompiledAssembly.GetTypes();
            _settingsType = generatedTypes.First(t => typeof(ApplicationSettingsBase).IsAssignableFrom(t));
            _proxies = generatedTypes.Where(t => t != _settingsType).ToDictionary(k => k.GetInterfaces().FirstOrDefault(t => t != typeof(INotifyPropertyChanged) && t != typeof(IXmlSerializable)), v => v);
        }

        [Conditional("DEBUG")]
        private static void TraceCodeGeneration(CSharpCodeProvider compiler, CodeCompileUnit compileUnit)
        {
            using (var writer = new StreamWriter(new FileStream("settings.internal_.cs", FileMode.Create)))
            {
                compiler.GenerateCodeFromCompileUnit(compileUnit, writer, new CodeGeneratorOptions { BracingStyle = "C" });
            }
        }

        private static CodeTypeDeclaration CreateType(Type modelType, CodeTypeDeclaration settingsClass)
        {
            if (!modelType.IsInterface)
                throw new InvalidOperationException($"Type '{modelType.Name}' is not an interface.");

            var className = modelType.Name;
            if (className.Length > 1 && className[0] == 'I' && className[1] >= 'A' && className[1] <= 'Z')
                className = className.Substring(1);
            var declaration = new CodeTypeDeclaration { Name = className, TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed };
            declaration.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute))));
            declaration.BaseTypes.Add(modelType);

            if (modelType.GetInterface(nameof(INotifyPropertyChanged)) == null)
                declaration.BaseTypes.Add(typeof(INotifyPropertyChanged));
            ImplementChangeNofication(declaration);

            if (modelType.GetInterface(nameof(IXmlSerializable)) == null)
                declaration.BaseTypes.Add(typeof(IXmlSerializable));
            ImplementSerialization(declaration);

            foreach (var property in modelType.GetProperties())
            {
                var propertyType = property.PropertyType;
                // We only support primitives, string, SecureString, Guid and ValueTypes that implement IConvertible
                if (!propertyType.IsPrimitive &&
                    propertyType != typeof(string) &&
                    propertyType != typeof(SecureString) &&
                    propertyType != typeof(Guid) &&
                    (!propertyType.IsValueType || !typeof(IConvertible).IsAssignableFrom(propertyType))) continue;

                // Only allow properties that has a getter and setter
                if (!property.CanRead || !property.CanWrite) continue;

                CreateNotificationProperty(declaration, property);
            }

            var settingsProperty = new CodeSnippetTypeMember($"        [{typeof(ApplicationScopedSettingAttribute).FullName}]{Environment.NewLine}" +
                                                             $"        public {className} {className} {{ get; set; }}");
            settingsClass.Members.Add(settingsProperty);

            return declaration;
        }

        private static void CreateNotificationProperty(CodeTypeDeclaration declaration, PropertyInfo propertyInfo)
        {
            var field = new CodeMemberField { Name = $"_{propertyInfo.Name}", Type = new CodeTypeReference(propertyInfo.PropertyType), Attributes = MemberAttributes.Private };
            declaration.Members.Add(field);

            var property = new CodeMemberProperty { Name = propertyInfo.Name, Type = new CodeTypeReference(propertyInfo.PropertyType), HasSet = true, HasGet = true };
            property.Attributes = (property.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;

            if (propertyInfo.PropertyType == typeof(SecureString))
            {
                var nullCheck = new CodeConditionStatement
                {
                    Condition = new CodeBinaryOperatorExpression
                    {
                        Left = new CodePrimitiveExpression(null),
                        Right = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), $"_{propertyInfo.Name}"),
                        Operator = CodeBinaryOperatorType.IdentityInequality
                    }
                };
                nullCheck.TrueStatements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), $"_{propertyInfo.Name}"), nameof(SecureString.Copy))));
                property.GetStatements.Add(nullCheck);
                property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
            }
            else
            {
                property.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), $"_{propertyInfo.Name}")));
            }

            var compare = new CodeMethodInvokeExpression { Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(Comparator)), nameof(Comparator.AreEqual)) };
            compare.Parameters.Add(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), $"_{propertyInfo.Name}"));
            compare.Parameters.Add(new CodePropertySetValueReferenceExpression());

            var condition = new CodeConditionStatement { Condition = new CodeBinaryOperatorExpression { Left = new CodePrimitiveExpression(false), Right = compare, Operator = CodeBinaryOperatorType.IdentityEquality } };
            var nullValueCheck = new CodeConditionStatement
            {
                Condition = new CodeBinaryOperatorExpression
                {
                    Left = new CodePrimitiveExpression(null),
                    Right = new CodePropertySetValueReferenceExpression(),
                    Operator = CodeBinaryOperatorType.IdentityInequality
                }
            };

            if (propertyInfo.PropertyType == typeof(SecureString))
            {
                condition.TrueStatements.Add(nullValueCheck);
                nullValueCheck.TrueStatements.Add(new CodeAssignStatement
                {
                    Left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), $"_{propertyInfo.Name}"),
                    Right = propertyInfo.PropertyType != typeof(SecureString)
                                                          ? (CodeExpression)new CodePropertySetValueReferenceExpression()
                                                          : new CodeMethodInvokeExpression { Method = new CodeMethodReferenceExpression(new CodePropertySetValueReferenceExpression(), nameof(SecureString.Copy)) }
                });
                nullValueCheck.FalseStatements.Add(new CodeAssignStatement
                {
                    Left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), $"_{propertyInfo.Name}"),
                    Right = new CodePrimitiveExpression(null)

                });
            }
            else
            {
                condition.TrueStatements.Add(new CodeAssignStatement
                {
                    Left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), $"_{propertyInfo.Name}"),
                    Right = propertyInfo.PropertyType != typeof(SecureString)
                                                          ? (CodeExpression)new CodePropertySetValueReferenceExpression()
                                                          : new CodeMethodInvokeExpression { Method = new CodeMethodReferenceExpression(new CodePropertySetValueReferenceExpression(), nameof(SecureString.Copy)) }
                });
            }

            var notify = new CodeMethodInvokeExpression { Method = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), PropertyChangedMethodName) };
            notify.Parameters.Add(new CodePrimitiveExpression(propertyInfo.Name));
            condition.TrueStatements.Add(notify);
            property.SetStatements.Add(condition);

            declaration.Members.Add(property);
        }

        private static void ImplementChangeNofication(CodeTypeDeclaration declaration)
        {
            var args = new CodeObjectCreateExpression { CreateType = new CodeTypeReference(typeof(PropertyChangedEventArgs)) };
            args.Parameters.Add(new CodeVariableReferenceExpression(PropertyChangedParameterName));

            var invoke = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), nameof(INotifyPropertyChanged.PropertyChanged));
            invoke.Parameters.Add(new CodeThisReferenceExpression());
            invoke.Parameters.Add(args);

            var condition = new CodeConditionStatement
            {
                Condition = new CodeBinaryOperatorExpression
                {
                    Left = new CodePrimitiveExpression(null),
                    Right = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), nameof(INotifyPropertyChanged.PropertyChanged)),
                    Operator = CodeBinaryOperatorType.IdentityInequality
                }
            };

            condition.TrueStatements.Add(invoke);

            var method = new CodeMemberMethod { Name = PropertyChangedMethodName, Attributes = MemberAttributes.Private };
            method.Parameters.Add(new CodeParameterDeclarationExpression { Name = PropertyChangedParameterName, Type = new CodeTypeReference(typeof(string)) });
            method.Statements.Add(condition);

            declaration.Members.Add(new CodeMemberEvent { Name = nameof(INotifyPropertyChanged.PropertyChanged), Attributes = MemberAttributes.Public, Type = new CodeTypeReference(typeof(PropertyChangedEventHandler)) });
            declaration.Members.Add(method);
        }

        private static void ImplementSerialization(CodeTypeDeclaration declaration)
        {
            // Generates
            // public XmlSchema GetSchema() { return null; }
            var getSchemaMethod = new CodeMemberMethod { Name = nameof(IXmlSerializable.GetSchema), ReturnType = new CodeTypeReference(typeof(XmlSchema)) };
            getSchemaMethod.Attributes = (getSchemaMethod.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            getSchemaMethod.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
            declaration.Members.Add(getSchemaMethod);

            // Generates
            // public void ReadXml(XmlReader reader) { SettingsXmlSerializer.DeserializeSettings(reader, this); }
            var readXmlMethod = new CodeMemberMethod { Name = nameof(IXmlSerializable.ReadXml) };
            var invokeReader = new CodeMethodInvokeExpression { Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(SettingsXmlSerializer)), nameof(SettingsXmlSerializer.DeserializeSettings)) };
            invokeReader.Parameters.Add(new CodeVariableReferenceExpression(ReadXmlParameterName));
            invokeReader.Parameters.Add(new CodeThisReferenceExpression());
            readXmlMethod.Attributes = (readXmlMethod.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            readXmlMethod.Statements.Add(invokeReader);
            readXmlMethod.Parameters.Add(new CodeParameterDeclarationExpression { Name = ReadXmlParameterName, Type = new CodeTypeReference(typeof(XmlReader)) });
            declaration.Members.Add(readXmlMethod);

            // Generates
            // public void WriteXml(XmlWriter writer) { SettingsXmlSerializer.SerializeSettings(writer, this); }
            var writeXmlMethod = new CodeMemberMethod { Name = nameof(IXmlSerializable.WriteXml) };
            var invokeWriter = new CodeMethodInvokeExpression { Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(SettingsXmlSerializer)), nameof(SettingsXmlSerializer.SerializeSettings)) };
            invokeWriter.Parameters.Add(new CodeVariableReferenceExpression(WriteXmlParameterName));
            invokeWriter.Parameters.Add(new CodeThisReferenceExpression());
            writeXmlMethod.Attributes = (writeXmlMethod.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            writeXmlMethod.Statements.Add(invokeWriter);
            writeXmlMethod.Parameters.Add(new CodeParameterDeclarationExpression { Name = WriteXmlParameterName, Type = new CodeTypeReference(typeof(XmlWriter)) });
            declaration.Members.Add(writeXmlMethod);
        }
    }
}
