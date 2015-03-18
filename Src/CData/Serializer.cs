using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections;

namespace CData {
    public static class Serializer {
        public static bool TryLoad(string filePath, TextReader reader, DiagContext context, ObjectTypeMetadata objectTypeMetadata, out object result) {
            return Parser.Parse(filePath, reader, context, objectTypeMetadata, out result);
        }
        public static void Save(object obj, ObjectTypeMetadata objectTypeMetadata, TextWriter writer, string indentString = "\t", string newLineString = "\n") {
            if (writer == null) throw new ArgumentNullException("writer");
            var sb = new StringBuilder(1024 * 2);
            Save(obj, objectTypeMetadata, sb, indentString, newLineString);
            writer.Write(sb.ToString());
        }
        public static void Save(object obj, ObjectTypeMetadata objectTypeMetadata, StringBuilder stringBuilder, string indentString = "\t", string newLineString = "\n") {
            if (obj == null) throw new ArgumentNullException("obj");
            if (objectTypeMetadata == null) throw new ArgumentNullException("objectTypeMetadata");
            if (stringBuilder == null) throw new ArgumentNullException("stringBuilder");
            SaveObject(true, obj, objectTypeMetadata, new SavingContext(stringBuilder, indentString, newLineString));
        }
        private static void SaveObject(bool isRoot, object obj, ObjectTypeMetadata objectMd, SavingContext context) {
            string rootAlias = null;
            if (isRoot) {
                rootAlias = context.AddUri(objectMd.FullName.Uri);
            }
            else {
                context.Append(objectMd.FullName);
            }
            var sb = context.StringBuilder;
            sb.Append(" {");
            context.AppendLine();
            context.PushIndent();
            List<PropertyMetadata> propMdList = null;
            objectMd.GetAllProperties(ref propMdList);
            if (propMdList != null) {
                foreach (var propMd in propMdList) {
                    context.Append(propMd.Name);
                    sb.Append(" = ");
                    SaveValue(propMd.GetValue(obj), propMd.Type, context);
                    context.AppendLine();
                }
            }
            context.PopIndent();
            context.Append('}');
            if (isRoot) {
                context.InsertRootObjectHead(rootAlias, objectMd.FullName.Name);
            }
        }
        private static void SaveValue(object value, TypeMetadata typeMd, SavingContext context) {
            if (value == null) {
                context.Append("null");
            }
            else {
                var typeKind = typeMd.Kind;
                if (typeKind.IsAtom()) {
                    context.Append(null);
                    SaveAtom(value, typeKind, context.StringBuilder);
                }
                else if (typeKind.IsObject()) {
                    SaveObject(false, value, (ObjectTypeMetadata)typeMd, context);
                }
                else if (typeKind.IsMap()) {
                    var collMd = (CollectionTypeMetadata)typeMd;
                    var keyMd = collMd.KeyType;
                    var itemMd = collMd.ItemType;
                    IEnumerator valueEnumerator = null;
                    context.Append("#[");
                    context.AppendLine();
                    context.PushIndent();
                    foreach (var key in collMd.GetMapKeys(value)) {
                        SaveValue(key, keyMd, context);
                        context.StringBuilder.Append(" = ");
                        if (valueEnumerator == null) {
                            valueEnumerator = collMd.GetMapValues(value).GetEnumerator();
                        }
                        valueEnumerator.MoveNext();
                        SaveValue(valueEnumerator.Current, itemMd, context);
                        context.AppendLine();
                    }
                    context.PopIndent();
                    context.Append(']');
                }
                else {
                    var itemMd = ((CollectionTypeMetadata)typeMd).ItemType;
                    context.Append('[');
                    context.AppendLine();
                    context.PushIndent();
                    foreach (var item in (IEnumerable)value) {
                        SaveValue(item, itemMd, context);
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
                    sb.Append(((BinaryValue)value).Value.ToInvString());
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
//a0:name <> {
//   prop1 = name {
//
//}