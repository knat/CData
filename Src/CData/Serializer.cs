using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections;

namespace CData {
    public static class Serializer {
        public static bool TryLoad<T>(string filePath, TextReader reader, DiagContext context, ClassMetadata classMetadata, out T result) where T : class {
            object obj;
            if (Parser.Parse(filePath, reader, context, classMetadata, out obj)) {
                result = (T)obj;
                return true;
            }
            result = null;
            return false;
        }
        public static void Save(object obj, ClassMetadata classMetadata, TextWriter writer, string indentString = "\t", string newLineString = "\n") {
            if (writer == null) throw new ArgumentNullException("writer");
            var sb = new StringBuilder(1024 * 2);
            Save(obj, classMetadata, sb, indentString, newLineString);
            writer.Write(sb.ToString());
        }
        public static void Save(object obj, ClassMetadata classMetadata, StringBuilder stringBuilder, string indentString = "\t", string newLineString = "\n") {
            if (obj == null) throw new ArgumentNullException("obj");
            if (classMetadata == null) throw new ArgumentNullException("classMetadata");
            if (stringBuilder == null) throw new ArgumentNullException("stringBuilder");
            SaveClassValue(true, obj, classMetadata, new SavingContext(stringBuilder, indentString, newLineString));
        }
        private static void SaveClassValue(bool isRoot, object obj, ClassMetadata clsMd, SavingContext context) {
            string rootAlias = null;
            if (isRoot) {
                rootAlias = context.AddUri(clsMd.FullName.Uri);
            }
            else {
                context.Append(clsMd.FullName);
            }
            var sb = context.StringBuilder;
            sb.Append(" {");
            context.AppendLine();
            context.PushIndent();
            var propMds = clsMd.GetPropertiesInHierarchy();
            if (propMds != null) {
                foreach (var propMd in propMds) {
                    context.Append(propMd.Name);
                    sb.Append(" = ");
                    SaveTypeValue(propMd.GetValue(obj), propMd.Type, context);
                    context.AppendLine();
                }
            }
            context.PopIndent();
            context.Append('}');
            if (isRoot) {
                context.InsertRootObjectHead(rootAlias, clsMd.FullName.Name);
            }
        }
        private static void SaveTypeValue(object value, TypeMetadata typeMd, SavingContext context) {
            if (value == null) {
                context.Append("null");
            }
            else {
                var typeKind = typeMd.Kind;
                if (typeKind.IsAtom()) {
                    context.Append(null);
                    SaveAtom(value, typeKind, context.StringBuilder);
                }
                else if (typeKind == TypeKind.Class) {
                    SaveClassValue(false, value, ((EntityRefTypeMetadata)typeMd).Entity as ClassMetadata, context);
                }
                else if (typeKind == TypeKind.Enum) {
                    context.Append('$');
                    var enumMd = ((EntityRefTypeMetadata)typeMd).Entity as EnumMetadata;
                    context.Append(enumMd.FullName);
                    var sb = context.StringBuilder;
                    sb.Append('.');
                    sb.Append(enumMd.GetMemberName(value) ?? "null");
                }
                else if (typeKind == TypeKind.Map) {
                    var collMd = (CollectionTypeMetadata)typeMd;
                    var keyMd = collMd.MapKeyType;
                    var itemMd = collMd.ItemOrValueType;
                    IEnumerator valueEnumerator = null;
                    context.Append("#[");
                    context.AppendLine();
                    context.PushIndent();
                    foreach (var key in collMd.GetMapKeys(value)) {
                        SaveTypeValue(key, keyMd, context);
                        context.StringBuilder.Append(" = ");
                        if (valueEnumerator == null) {
                            valueEnumerator = collMd.GetMapValues(value).GetEnumerator();
                        }
                        valueEnumerator.MoveNext();
                        SaveTypeValue(valueEnumerator.Current, itemMd, context);
                        context.AppendLine();
                    }
                    context.PopIndent();
                    context.Append(']');
                }
                else {
                    var itemMd = ((CollectionTypeMetadata)typeMd).ItemOrValueType;
                    context.Append('[');
                    context.AppendLine();
                    context.PushIndent();
                    foreach (var item in (IEnumerable)value) {
                        SaveTypeValue(item, itemMd, context);
                        context.AppendLine();
                    }
                    context.PopIndent();
                    context.Append(']');
                }
            }
        }
        private static void SaveAtom(object value, TypeKind typeKind, StringBuilder sb) {
            switch (typeKind) {
                case TypeKind.String:
                    Extensions.GetLiteral(((string)value), sb);
                    break;
                case TypeKind.IgnoreCaseString:
                    Extensions.GetLiteral(((IgnoreCaseString)value).Value, sb);
                    break;
                case TypeKind.Int64:
                    sb.Append(((long)value).ToInvString());
                    break;
                case TypeKind.Int32:
                    sb.Append(((int)value).ToInvString());
                    break;
                case TypeKind.Int16:
                    sb.Append(((short)value).ToInvString());
                    break;
                case TypeKind.SByte:
                    sb.Append(((sbyte)value).ToInvString());
                    break;
                case TypeKind.UInt64:
                    sb.Append(((ulong)value).ToInvString());
                    break;
                case TypeKind.UInt32:
                    sb.Append(((uint)value).ToInvString());
                    break;
                case TypeKind.UInt16:
                    sb.Append(((ushort)value).ToInvString());
                    break;
                case TypeKind.Byte:
                    sb.Append(((byte)value).ToInvString());
                    break;
                case TypeKind.Double: {
                        bool isLiteral;
                        var s = ((double)value).ToInvString(out isLiteral);
                        if (isLiteral) {
                            Extensions.GetLiteral(s, sb);
                        }
                        else {
                            sb.Append(s);
                        }
                    }
                    break;
                case TypeKind.Single: {
                        bool isLiteral;
                        var s = ((float)value).ToInvString(out isLiteral);
                        if (isLiteral) {
                            Extensions.GetLiteral(s, sb);
                        }
                        else {
                            sb.Append(s);
                        }
                    }
                    break;
                case TypeKind.Boolean:
                    sb.Append(((bool)value).ToInvString());
                    break;
                case TypeKind.Binary:
                    sb.Append('"');
                    sb.Append(((Binary)value).ToBase64String());
                    sb.Append('"');
                    break;
                case TypeKind.Guid:
                    sb.Append('"');
                    sb.Append(((Guid)value).ToInvString());
                    sb.Append('"');
                    break;
                case TypeKind.TimeSpan:
                    sb.Append('"');
                    sb.Append(((TimeSpan)value).ToInvString());
                    sb.Append('"');
                    break;
                case TypeKind.DateTimeOffset:
                    sb.Append('"');
                    sb.Append(((DateTimeOffset)value).ToInvString());
                    sb.Append('"');
                    break;

                default:
                    throw new ArgumentException("Invalid type kind: " + typeKind.ToString());
            }
        }

    }
}
