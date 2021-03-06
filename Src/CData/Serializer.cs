﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections;

namespace CData {
    public static class Serializer {
        public static bool TryLoad<T>(string filePath, TextReader reader, DiagContext context,
            AssemblyMetadata assemblyMetadata, ClassMetadata classMetadata, out T result) where T : class {
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
            var sb = StringBuilderBuffer.Acquire();
            Save(obj, classMetadata, sb, indentString, newLineString);
            writer.Write(sb.ToStringAndRelease());
        }
        public static void Save(object obj, ClassMetadata classMetadata, StringBuilder stringBuilder, string indentString = "\t", string newLineString = "\n") {
            if (obj == null) throw new ArgumentNullException("obj");
            if (classMetadata == null) throw new ArgumentNullException("classMetadata");
            SaveClassValue(true, obj, classMetadata, new SavingContext(stringBuilder, indentString, newLineString));
        }
        private static void SaveClassValue(bool isRoot, object obj, ClassMetadata clsMd, SavingContext context) {
            string rootAlias = null;
            if (isRoot) {
                rootAlias = context.AddUri(clsMd.FullName.Uri);
            }
            else {
                clsMd = clsMd.GetMetadata(obj);
                context.AppendFullName(clsMd.FullName);
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
                    SaveLocalValue(propMd.GetValue(obj), propMd.Type, context);
                    context.AppendLine();
                }
            }
            context.PopIndent();
            context.Append('}');
            if (isRoot) {
                context.InsertRootObjectHead(rootAlias, clsMd.FullName.Name);
            }
        }
        private static void SaveLocalValue(object value, LocalTypeMetadata typeMd, SavingContext context) {
            if (value == null) {
                context.Append("null");
            }
            else {
                var typeKind = typeMd.Kind;
                if (typeKind.IsAtom()) {
                    context.Append(null);
                    SaveAtomValue(value, typeKind, context.StringBuilder);
                }
                else if (typeKind == TypeKind.Class) {
                    SaveClassValue(false, value, ((GlobalTypeRefMetadata)typeMd).GlobalType as ClassMetadata, context);
                }
                else if (typeKind == TypeKind.Enum) {
                    var enumMd = ((GlobalTypeRefMetadata)typeMd).GlobalType as EnumMetadata;
                    var memberName = enumMd.GetMemberName(value);
                    if (memberName == null) {
                        context.Append("null");
                    }
                    else {
                        context.Append('$');
                        context.AppendFullName(enumMd.FullName);
                        var sb = context.StringBuilder;
                        sb.Append('.');
                        sb.Append(memberName);
                    }
                }
                else if (typeKind == TypeKind.Map) {
                    var collMd = (CollectionMetadata)typeMd;
                    var keyMd = collMd.MapKeyType;
                    var valueMd = collMd.ItemOrValueType;
                    context.Append("#[");
                    context.AppendLine();
                    context.PushIndent();
                    IDictionaryEnumerator mapEnumerator = collMd.GetMapEnumerator(value);
                    if (mapEnumerator != null) {
                        while (mapEnumerator.MoveNext()) {
                            SaveLocalValue(mapEnumerator.Key, keyMd, context);
                            context.StringBuilder.Append(" = ");
                            SaveLocalValue(mapEnumerator.Value, valueMd, context);
                            context.AppendLine();
                        }
                        var disposable = mapEnumerator as IDisposable;
                        if (disposable != null) {
                            disposable.Dispose();
                        }
                    }
                    context.PopIndent();
                    context.Append(']');
                }
                else {
                    var itemMd = ((CollectionMetadata)typeMd).ItemOrValueType;
                    context.Append('[');
                    context.AppendLine();
                    context.PushIndent();
                    foreach (var item in (IEnumerable)value) {
                        SaveLocalValue(item, itemMd, context);
                        context.AppendLine();
                    }
                    context.PopIndent();
                    context.Append(']');
                }
            }
        }
        private static void SaveAtomValue(object value, TypeKind typeKind, StringBuilder sb) {
            switch (typeKind) {
                case TypeKind.String:
                    AtomExtensions.GetLiteral((string)value, sb);
                    break;
                case TypeKind.IgnoreCaseString:
                    AtomExtensions.GetLiteral(((IgnoreCaseString)value).Value, sb);
                    break;
                case TypeKind.Char:
                    AtomExtensions.GetLiteral((char)value, sb);
                    break;
                case TypeKind.Decimal:
                    sb.Append(((decimal)value).ToInvString());
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
                            AtomExtensions.GetLiteral(s, sb);
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
                            AtomExtensions.GetLiteral(s, sb);
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
