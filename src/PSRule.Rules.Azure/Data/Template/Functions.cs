﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSRule.Rules.Azure.Resources;
using static PSRule.Rules.Azure.Data.Template.TemplateVisitor;

namespace PSRule.Rules.Azure.Data.Template
{
    /// <summary>
    /// Implementation of Azure Resource Manager template functions as ExpressionFn.
    /// </summary>
    internal static class Functions
    {
        internal readonly static IFunctionDescriptor[] Builtin = new IFunctionDescriptor[]
        {
            // Array and object
            new FunctionDescriptor("array", Array),
            new FunctionDescriptor("concat", Concat),
            new FunctionDescriptor("contains", Contains),
            new FunctionDescriptor("createArray", CreateArray),
            new FunctionDescriptor("createObject", CreateObject),
            new FunctionDescriptor("empty", Empty),
            new FunctionDescriptor("first", First),
            new FunctionDescriptor("intersection", Intersection),
            new FunctionDescriptor("last", Last),
            new FunctionDescriptor("length", Length),
            new FunctionDescriptor("min", Min),
            new FunctionDescriptor("max", Max),
            new FunctionDescriptor("range", Range),
            new FunctionDescriptor("skip", Skip),
            new FunctionDescriptor("take", Take),
            new FunctionDescriptor("union", Union),

            // Comparison
            new FunctionDescriptor("coalesce", Coalesce),
            new FunctionDescriptor("equals", Equals),
            new FunctionDescriptor("greater", Greater),
            new FunctionDescriptor("greaterOrEquals", GreaterOrEquals),
            new FunctionDescriptor("less", Less),
            new FunctionDescriptor("lessOrEquals", LessOrEquals),

            // Date
            new FunctionDescriptor("dateTimeAdd", DateTimeAdd),
            new FunctionDescriptor("utcNow", UtcNow),

            // Deployment
            new FunctionDescriptor("deployment", Deployment),
            new FunctionDescriptor("environment", Environment),
            new FunctionDescriptor("parameters", Parameters),
            new FunctionDescriptor("variables", Variables),

            // Logical
            new FunctionDescriptor("and", And, delayBinding: true),
            new FunctionDescriptor("bool", Bool),
            new FunctionDescriptor("false", False),
            new FunctionDescriptor("if", If, delayBinding: true),
            new FunctionDescriptor("not", Not),
            new FunctionDescriptor("or", Or),
            new FunctionDescriptor("true", True),

            // Numeric
            new FunctionDescriptor("add", Add),
            new FunctionDescriptor("copyIndex", CopyIndex),
            new FunctionDescriptor("div", Div),
            new FunctionDescriptor("float", Float),
            new FunctionDescriptor("int", Int),
            // min - also in array and object
            // max - also in array and object
            new FunctionDescriptor("mod", Mod),
            new FunctionDescriptor("mul", Mul),
            new FunctionDescriptor("sub", Sub),

            // Object
            new FunctionDescriptor("json", Json),
            new FunctionDescriptor("null", Null),

            // Resource
            new FunctionDescriptor("extensionResourceId", ExtensionResourceId),
            new FunctionDescriptor("list", List), // Includes listAccountSas, listKeys, listSecrets, list*
            // pickZones
            new FunctionDescriptor("providers", Providers),
            new FunctionDescriptor("reference", Reference),
            new FunctionDescriptor("resourceGroup", ResourceGroup),
            new FunctionDescriptor("resourceId", ResourceId),
            new FunctionDescriptor("subscription", Subscription),
            new FunctionDescriptor("subscriptionResourceId", SubscriptionResourceId),
            new FunctionDescriptor("tenantResourceId", TenantResourceId),

            // String
            new FunctionDescriptor("base64", Base64),
            new FunctionDescriptor("base64ToJson", Base64ToJson),
            new FunctionDescriptor("base64ToString", Base64ToString),
            // concat - also in array and object
            // contains - also in array and object
            new FunctionDescriptor("dataUri", DataUri),
            new FunctionDescriptor("dataUriToString", DataUriToString),
            // empty - also in array and object
            new FunctionDescriptor("endsWith", EndsWith),
            // first - also in array and object
            new FunctionDescriptor("format", Format),
            new FunctionDescriptor("guid", Guid),
            new FunctionDescriptor("indexOf", IndexOf),
            // last - also in array and object
            new FunctionDescriptor("lastIndexOf", LastIndexOf),
            // length - also in array and object
            new FunctionDescriptor("newGuid", NewGuid),
            new FunctionDescriptor("padLeft", PadLeft),
            new FunctionDescriptor("replace", Replace),
            // skip - also in array and object
            new FunctionDescriptor("split", Split),
            new FunctionDescriptor("startsWith", StartsWith),
            new FunctionDescriptor("string", String),
            new FunctionDescriptor("substring", Substring),
            // take - also in array and object
            new FunctionDescriptor("toLower", ToLower),
            new FunctionDescriptor("toUpper", ToUpper),
            new FunctionDescriptor("trim", Trim),
            new FunctionDescriptor("uniqueString", UniqueString),
            new FunctionDescriptor("uri", Uri),
            new FunctionDescriptor("uriComponent", UriComponent),
            new FunctionDescriptor("uriComponentToString", UriComponentToString),
        };

        private static readonly CultureInfo AzureCulture = new CultureInfo("en-US");

        #region Array and object

        internal static object Array(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(Array), args);

            if (TryJArray(args[0], out JArray jArray))
                return jArray;

            return new JArray(args[0]);
        }

        internal static object Coalesce(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) < 1)
                throw ArgumentsOutOfRange(nameof(Coalesce), args);

            for (var i = 0; i < args.Length; i++)
            {
                if (!IsNull(args[i]))
                    return args[i];
            }
            return args[0];
        }

        internal static object Concat(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) < 1)
                throw ArgumentsOutOfRange(nameof(Concat), args);

            // String
            if (ExpressionHelpers.TryConvertStringArray(args, out string[] s))
            {
                return string.Concat(s);
            }
            // Array
            else if (args[0] is Array || args[0] is JArray)
            {
                var result = new List<object>();
                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] is Array array)
                    {
                        for (var j = 0; j < array.Length; j++)
                            result.Add(array.GetValue(j));
                    }
                    else if (args[i] is JArray jArray)
                    {
                        for (var j = 0; j < jArray.Count; j++)
                            result.Add(jArray[j]);
                    }
                }
                return result.ToArray();
            }
            throw ArgumentFormatInvalid(nameof(Concat));
        }

        internal static Array Concat(Array[] array)
        {
            var result = new List<object>();
            for (var i = 0; i < array.Length; i++)
            {
                for (var j = 0; j < array[i].Length; j++)
                    result.Add(array[i].GetValue(j));
            }
            return result.ToArray();
        }

        internal static object Empty(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 1)
                throw ArgumentsOutOfRange(nameof(Empty), args);

            if (args[0] == null)
                return true;
            else if (args[0] is Array aValue)
                return aValue.Length == 0;
            else if (TryJArray(args[0], out JArray jArray))
                return jArray.Count == 0;
            else if (ExpressionHelpers.TryString(args[0], out string sValue))
                return string.IsNullOrEmpty(sValue);
            else if (TryJObject(args[0], out JObject jObject))
                return !jObject.Properties().Any();

            return false;
        }

        internal static object Contains(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 2)
                throw ArgumentsOutOfRange(nameof(Contains), args);

            var objectToFind = args[1];

            if (args[0] is Array avalue)
                return Contains(avalue, objectToFind);
            else if (args[0] is JArray jArray)
                return jArray.Contains(JToken.FromObject(objectToFind));
            else if (args[0] is string svalue)
                return svalue.Contains(objectToFind.ToString());
            else if (args[0] is JObject jObject)
                return jObject.ContainsKey(objectToFind.ToString());

            return false;
        }

        /// <summary>
        /// createArray (arg1, arg2, arg3, ...)
        /// </summary>
        internal static object CreateArray(ITemplateContext context, object[] args)
        {
            return (args == null || args.Length == 0) ? new JArray() : new JArray(args);
        }

        /// <summary>
        /// createObject(key1, value1, key2, value2, ...)
        /// </summary>
        internal static object CreateObject(ITemplateContext context, object[] args)
        {
            var argCount = CountArgs(args);
            if (argCount % 2 != 0)
                throw ArgumentsOutOfRange(nameof(CreateObject), args);

            var properties = new JProperty[argCount / 2];
            for (var i = 0; i < argCount / 2; i++)
            {
                if (!ExpressionHelpers.TryString(args[i * 2], out string name))
                    throw ArgumentInvalidString(nameof(CreateObject), $"key{i + 1}");

                properties[i] = new JProperty(name, args[i * 2 + 1]);
            }
            return new JObject(properties);
        }

        internal static object First(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 1)
                throw ArgumentsOutOfRange(nameof(First), args);

            if (args[0] is Array avalue)
                return avalue.GetValue(0);
            else if (args[0] is JArray jArray)
                return jArray[0];
            else if (ExpressionHelpers.TryString(args[0], out string svalue))
                return new string(svalue[0], 1);

            return null;
        }

        internal static object Intersection(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) < 2)
                throw ArgumentsOutOfRange(nameof(Intersection), args);

            // Array
            if (args[0] is JArray jArray)
            {
                IEnumerable<JToken> intersection = jArray;
                for (var i = 1; i < args.Length; i++)
                {
                    if (!TryJArray(args[i], out JArray value))
                        throw new ArgumentException();

                    intersection = intersection.Intersect(value);
                }
                return new JArray(intersection.ToArray());
            }

            // Object
            if (args[0] is JObject jObject)
            {
                var intersection = jObject.DeepClone() as JObject;
                for (var i = 1; i < args.Length; i++)
                {
                    if (!TryJObject(args[i], out JObject value))
                        throw new ArgumentException();

                    foreach (var prop in intersection.Properties().ToArray())
                    {
                        if (!(value.ContainsKey(prop.Name) && JToken.DeepEquals(value[prop.Name], prop.Value)))
                            intersection.Remove(prop.Name);
                    }
                }
                return intersection;
            }
            throw new ArgumentException();
        }

        internal static object Json(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 1 || !ExpressionHelpers.TryString(args[0], out string json))
                throw new ArgumentOutOfRangeException();

            return JsonConvert.DeserializeObject(json);
        }

        /// <summary>
        /// null()
        /// </summary>
        internal static object Null(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) > 0)
                throw ArgumentsOutOfRange(nameof(Null), args);

            return null;
        }

        internal static object Last(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 1)
                throw ArgumentsOutOfRange(nameof(Last), args);

            if (args[0] is Array avalue)
                return avalue.GetValue(avalue.Length - 1);
            else if (args[0] is JArray jArray)
                return jArray[jArray.Count - 1];
            else if (ExpressionHelpers.TryString(args[0], out string svalue))
                return new string(svalue[svalue.Length - 1], 1);

            return null;
        }

        internal static object Length(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(Length), args);

            if (ExpressionHelpers.TryString(args[0], out string s))
                return (long)s.Length;
            else if (args[0] is Array a)
                return (long)a.Length;
            else if (args[0] is JArray jArray)
                return (long)jArray.Count;

            return (long)args[0].GetType().GetProperties().Length;
        }

        internal static object Min(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) == 0)
                throw ArgumentsOutOfRange(nameof(Min), args);

            long? result = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (ExpressionHelpers.TryLong(args[i], out long value))
                {
                    result = !result.HasValue || value < result ? value : result;
                }
                // Enumerate array arg
                else if (TryJArray(args[i], out JArray array))
                {
                    for (var j = 0; j < array.Count; j++)
                    {
                        if (ExpressionHelpers.TryLong(array[j], out value))
                        {
                            result = !result.HasValue || value < result ? value : result;
                        }
                        else
                            throw new ArgumentException();
                    }
                }
                else
                    throw new ArgumentException();
            }
            return result;
        }

        internal static object Max(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) == 0)
                throw ArgumentsOutOfRange(nameof(Max), args);

            long? result = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (ExpressionHelpers.TryLong(args[i], out long value))
                {
                    result = !result.HasValue || value > result ? value : result;
                }
                // Enumerate array arg
                else if (TryJArray(args[i], out JArray array))
                {
                    for (var j = 0; j < array.Count; j++)
                    {
                        if (ExpressionHelpers.TryLong(array[j], out value))
                        {
                            result = !result.HasValue || value > result ? value : result;
                        }
                        else
                            throw new ArgumentException();
                    }
                }
                else
                    throw new ArgumentException();
            }
            return result;
        }

        internal static object Range(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 2)
                throw ArgumentsOutOfRange(nameof(Range), args);

            if (!ExpressionHelpers.TryLong(args[0], out long startIndex))
                throw ArgumentInvalidInteger(nameof(Range), nameof(startIndex));

            if (!ExpressionHelpers.TryLong(args[1], out long count))
                throw ArgumentInvalidInteger(nameof(Range), nameof(count));

            var result = new long[count];
            for (var i = 0; i < count; i++)
                result[i] = startIndex++;

            return new JArray(result);
        }

        internal static object Skip(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 2)
                throw ArgumentsOutOfRange(nameof(Skip), args);

            if (!ExpressionHelpers.TryInt(args[1], out int numberToSkip))
                throw ArgumentInvalidInteger(nameof(Skip), nameof(numberToSkip));

            int skip = numberToSkip <= 0 ? 0 : numberToSkip;
            if (ExpressionHelpers.TryString(args[0], out string soriginalValue))
            {
                if (skip >= soriginalValue.Length)
                    return string.Empty;

                return soriginalValue.Substring(skip);
            }
            else if (TryJArray(args[0], out JArray aoriginalvalue))
            {
                if (skip >= aoriginalvalue.Count)
                    return new JArray();

                var result = new JArray();
                for (var i = skip; i < aoriginalvalue.Count; i++)
                    result.Add(aoriginalvalue[i]);

                return result;
            }
            throw new ArgumentException();
        }

        internal static object Take(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 2)
                throw ArgumentsOutOfRange(nameof(Take), args);

            if (!ExpressionHelpers.TryInt(args[1], out int numberToTake))
                throw new ArgumentException();

            int take = numberToTake <= 0 ? 0 : numberToTake;
            if (ExpressionHelpers.TryString(args[0], out string soriginalValue))
            {
                if (take <= 0)
                    return string.Empty;

                take = take > soriginalValue.Length ? soriginalValue.Length : take;
                return soriginalValue.Substring(0, take);
            }
            else if (TryJArray(args[0], out JArray aoriginalvalue))
            {
                if (take <= 0)
                    return new JArray();

                var result = new JArray();
                for (var i = 0; i < aoriginalvalue.Count && i < take; i++)
                    result.Add(aoriginalvalue[i]);

                return result;
            }

            throw new ArgumentException();
        }

        internal static object Union(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) < 2)
                throw ArgumentsOutOfRange(nameof(Union), args);

            // Array
            if (args[0] is Array)
            {
                Array[] arrays = new Array[args.Length];
                args.CopyTo(arrays, 0);
                return Union(arrays);
            }
            else if (args[0] is JArray)
            {
                JArray[] arrays = new JArray[args.Length];
                for (var i = 0; i < arrays.Length; i++)
                {
                    arrays[i] = args[i] as JArray;
                }
                return Union(arrays);
            }

            // Object
            if (args[0] is JObject jObject1)
            {
                var result = new JObject(jObject1);
                for (var i = 1; i < args.Length && args[i] is JObject jObject2; i++)
                {
                    foreach (var property in jObject2.Properties())
                    {
                        if (!result.ContainsKey(property.Name))
                            result.Add(property.Name, property.Value);
                    }
                }
                return result;
            }
            return null;
        }

        private static Array Union(Array[] arrays)
        {
            var result = new List<object>();
            for (var i = 0; i < arrays.Length; i++)
            {
                for (var j = 0; arrays[i] != null && j < arrays[i].Length; j++)
                {
                    var value = arrays[i].GetValue(j);
                    if (!result.Contains(value))
                        result.Add(value);
                }
            }
            return result.ToArray();
        }

        private static JArray Union(JArray[] arrays)
        {
            var result = new JArray();
            for (var i = 0; i < arrays.Length; i++)
            {
                for (var j = 0; j < arrays[i].Count; j++)
                {
                    var element = arrays[i][j];
                    if (!result.Contains(element))
                        result.Add(element);
                }

            }
            return result;
        }

        #endregion Array and object

        #region Deployment

        /// <summary>
        /// deployment()
        /// </summary>
        internal static object Deployment(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) > 0)
                throw ArgumentsOutOfRange(nameof(Deployment), args);

            return context.Deployment;
        }

        /// <summary>
        /// environment()
        /// </summary>
        internal static object Environment(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) > 0)
                throw ArgumentsOutOfRange(nameof(Environment), args);

            return JObject.FromObject(context.GetEnvironment());
        }

        /// <summary>
        /// parameters(parameterName)
        /// </summary>
        internal static object Parameters(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(Parameters), args);

            if (!ExpressionHelpers.TryString(args[0], out string parameterName))
                throw ArgumentFormatInvalid(nameof(Parameters));

            if (!context.TryParameter(parameterName, out object result))
                throw new KeyNotFoundException(string.Format(Thread.CurrentThread.CurrentCulture, PSRuleResources.ParameterNotFound, parameterName));

            return result;
        }

        /// <summary>
        /// variables(variableName)
        /// </summary>
        internal static object Variables(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(Variables), args);

            if (!ExpressionHelpers.TryString(args[0], out string variableName))
                throw ArgumentFormatInvalid(nameof(Variables));

            if (!context.TryVariable(variableName, out object result))
                throw new KeyNotFoundException(string.Format(Thread.CurrentThread.CurrentCulture, PSRuleResources.VariableNotFound, variableName));

            return result;
        }

        #endregion Deployment

        #region Resource

        /// <summary>
        /// extensionResourceId(resourceId, resourceType, resourceName1, [resourceName2], ...)
        /// </summary>
        /// <returns>
        /// {scope}/providers/{extensionResourceProviderNamespace}/{extensionResourceType}/{extensionResourceName}
        /// </returns>
        internal static object ExtensionResourceId(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) < 3)
                throw ArgumentsOutOfRange(nameof(ExtensionResourceId), args);

            var segments = new string[args.Length];
            for (var i = 0; i < segments.Length; i++)
            {
                if (!ExpressionHelpers.TryString(args[i], out string value))
                    throw ArgumentFormatInvalid(nameof(ExtensionResourceId));

                segments[i] = value;
            }

            var resourceId = segments[0];
            if (!segments[1].Contains('/'))
                throw new ArgumentException();

            var resourceType = TrimResourceType(segments[1]);
            var nameDepth = resourceType.Split('/').Length - 1;
            if ((segments.Length - 2) != nameDepth)
                throw new TemplateFunctionException(nameof(ExtensionResourceId), FunctionErrorType.MismatchingResourceSegments, PSRuleResources.MismatchingResourceSegments);

            var name = new string[nameDepth];
            System.Array.Copy(segments, 2, name, 0, nameDepth);
            var nameParts = string.Join("/", name);
            return string.Concat(resourceId, "/providers/", resourceType, "/", nameParts);
        }

        /// <summary>
        /// list{Value}(resourceName or resourceIdentifier, apiVersion, functionValues)
        /// </summary>
        internal static object List(ITemplateContext context, object[] args)
        {
            var argCount = CountArgs(args);
            if (argCount < 2 || argCount > 3)
                throw ArgumentsOutOfRange(nameof(List), args);

            ExpressionHelpers.TryString(args[0], out string resourceId);
            return new MockList(resourceId);
        }

        internal static object Providers(ITemplateContext context, object[] args)
        {
            var argCount = CountArgs(args);
            if (argCount < 1 || argCount > 2)
                throw ArgumentsOutOfRange(nameof(Providers), args);

            if (!ExpressionHelpers.TryString(args[0], out string providerNamespace))
                throw ArgumentFormatInvalid(nameof(Providers));

            string resourceType = null;
            if (argCount > 1 && !ExpressionHelpers.TryString(args[1], out resourceType))
                throw ArgumentFormatInvalid(nameof(Providers));

            var resourceTypes = context.GetResourceType(providerNamespace, resourceType);
            if (resourceType == null)
                return resourceTypes;

            if (resourceTypes == null || resourceTypes.Length == 0)
                throw ArgumentInvalidResourceType(nameof(Providers), providerNamespace, resourceType);

            return resourceTypes[0];
        }

        internal static object Reference(ITemplateContext context, object[] args)
        {
            var argCount = CountArgs(args);
            if (argCount < 1 || argCount > 3)
                throw ArgumentsOutOfRange(nameof(Reference), args);

            ExpressionHelpers.TryString(args[0], out string resourceType);
            return new MockResource(resourceType);
        }

        /// <summary>
        /// resourceGroup()
        /// </summary>
        internal static object ResourceGroup(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) > 0)
                throw ArgumentsOutOfRange(nameof(ResourceGroup), args);

            return context.ResourceGroup;
        }

        /// <summary>
        /// resourceId([subscriptionId], [resourceGroupName], resourceType, resourceName1, [resourceName2], ...)
        /// </summary>
        /// <returns>
        /// /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{resourceProviderNamespace}/{resourceType}/{resourceName}
        /// </returns>
        internal static object ResourceId(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) < 2)
                throw ArgumentsOutOfRange(nameof(ResourceId), args);

            var segments = new string[args.Length];
            for (var i = 0; i < segments.Length; i++)
            {
                if (!ExpressionHelpers.TryString(args[i], out string value))
                    throw ArgumentFormatInvalid(nameof(ResourceId));

                segments[i] = value;
            }

            string subscriptionId = context.Subscription.SubscriptionId;
            string resourceGroup = context.ResourceGroup.Name;
            string resourceType = null;
            string nameParts = null;

            for (var i = 0; resourceType == null && i < segments.Length; i++)
            {
                if (segments[i].Contains('/'))
                {
                    // Copy earlier segments
                    if (i == 1)
                        resourceGroup = segments[0];
                    else if (i == 2)
                    {
                        resourceGroup = segments[1];
                        subscriptionId = segments[0];
                    }
                    resourceType = TrimResourceType(segments[i]);
                    var nameDepth = resourceType.Split('/').Length - 1;

                    if ((segments.Length - 1 - i) != nameDepth)
                        throw new TemplateFunctionException(nameof(ResourceId), FunctionErrorType.MismatchingResourceSegments, PSRuleResources.MismatchingResourceSegments);

                    string[] name = new string[nameDepth];
                    System.Array.Copy(segments, i + 1, name, 0, nameDepth);
                    nameParts = string.Join("/", name);
                }
            }
            return string.Concat("/subscriptions/", subscriptionId, "/resourceGroups/", resourceGroup, "/providers/", resourceType, "/", nameParts);
        }

        /// <summary>
        /// subscription()
        /// </summary>
        internal static object Subscription(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) > 0)
                throw ArgumentsOutOfRange(nameof(Subscription), args);

            return context.Subscription;
        }

        /// <summary>
        /// subscriptionResourceId([subscriptionId], resourceType, resourceName1, [resourceName2], ...)
        /// </summary>
        /// <returns>
        /// /subscriptions/{subscriptionId}/providers/{resourceProviderNamespace}/{resourceType}/{resourceName}
        /// </returns>
        internal static object SubscriptionResourceId(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) < 2)
                throw ArgumentsOutOfRange(nameof(SubscriptionResourceId), args);

            var segments = new string[args.Length];
            for (var i = 0; i < segments.Length; i++)
            {
                if (!ExpressionHelpers.TryString(args[i], out string value))
                    throw ArgumentFormatInvalid(nameof(SubscriptionResourceId));

                segments[i] = value;
            }

            string subscriptionId = context.Subscription.SubscriptionId;
            string resourceType = null;
            string nameParts = null;

            for (var i = 0; resourceType == null && i < segments.Length; i++)
            {
                if (segments[i].Contains('/'))
                {
                    // Copy earlier segments
                    if (i == 1)
                        subscriptionId = segments[0];

                    resourceType = TrimResourceType(segments[i]);
                    var nameDepth = resourceType.Split('/').Length - 1;

                    if ((segments.Length - 1 - i) != nameDepth)
                        throw new TemplateFunctionException(nameof(SubscriptionResourceId), FunctionErrorType.MismatchingResourceSegments, PSRuleResources.MismatchingResourceSegments);

                    string[] name = new string[nameDepth];
                    System.Array.Copy(segments, i + 1, name, 0, nameDepth);
                    nameParts = string.Join("/", name);
                }
            }
            return string.Concat("/subscriptions/", subscriptionId, "/providers/", resourceType, "/", nameParts);
        }

        /// <summary>
        /// tenantResourceId(resourceType, resourceName1, [resourceName2], ...)
        /// </summary>
        /// <returns>
        /// /providers/{resourceProviderNamespace}/{resourceType}/{resourceName}
        /// </returns>
        internal static object TenantResourceId(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) < 2)
                throw ArgumentsOutOfRange(nameof(TenantResourceId), args);

            var segments = new string[args.Length];
            for (var i = 0; i < segments.Length; i++)
            {
                if (!ExpressionHelpers.TryString(args[i], out string value))
                    throw ArgumentFormatInvalid(nameof(TenantResourceId));

                segments[i] = value;
            }

            string resourceType = null;
            string nameParts = null;

            for (var i = 0; resourceType == null && i < segments.Length; i++)
            {
                if (segments[i].Contains('/'))
                {
                    resourceType = TrimResourceType(segments[i]);
                    var nameDepth = resourceType.Split('/').Length - 1;

                    if ((segments.Length - 1 - i) != nameDepth)
                        throw new TemplateFunctionException(nameof(TenantResourceId), FunctionErrorType.MismatchingResourceSegments, PSRuleResources.MismatchingResourceSegments);

                    string[] name = new string[nameDepth];
                    System.Array.Copy(segments, i + 1, name, 0, nameDepth);
                    nameParts = string.Join("/", name);
                }
            }
            return string.Concat("/providers/", resourceType, "/", nameParts);
        }

        #endregion Resource

        #region Numeric

        internal static object Add(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 2)
                throw ArgumentsOutOfRange(nameof(Add), args);

            if (!ExpressionHelpers.TryConvertLong(args[0], out long operand1))
                throw ArgumentInvalidInteger(nameof(Add), "operand1");

            if (!ExpressionHelpers.TryConvertLong(args[1], out long operand2))
                throw ArgumentInvalidInteger(nameof(Add), "operand2");

            return operand1 + operand2;
        }

        internal static object CopyIndex(ITemplateContext context, object[] args)
        {
            string loopName = CountArgs(args) >= 1 && ExpressionHelpers.TryString(args[0], out string svalue) ? svalue : null;
            int offset = CountArgs(args) == 1 && ExpressionHelpers.TryConvertInt(args[0], out int ivalue) ? ivalue : 0;
            if (CountArgs(args) == 2 && offset == 0 && ExpressionHelpers.TryConvertInt(args[1], out int ivalue2))
                offset = ivalue2;

            if (!context.CopyIndex.TryGetValue(loopName, out TemplateContext.CopyIndexState value))
                throw new ArgumentException();

            return offset + value.Index;
        }

        internal static object Div(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 2)
                throw ArgumentsOutOfRange(nameof(Div), args);

            if (!ExpressionHelpers.TryConvertLong(args[0], out long operand1))
                throw ArgumentInvalidInteger(nameof(Div), "operand1");

            if (!ExpressionHelpers.TryConvertLong(args[1], out long operand2))
                throw ArgumentInvalidInteger(nameof(Div), "operand2");

            if (operand2 == 0)
                throw new DivideByZeroException();

            return operand1 / operand2;
        }

        internal static object Float(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(Float), args);

            if (ExpressionHelpers.TryConvertLong(args[0], out long ivalue))
                return (float)ivalue;
            else if (ExpressionHelpers.TryString(args[0], out string svalue))
                return float.Parse(svalue, new CultureInfo("en-us"));

            throw ArgumentInvalidInteger(nameof(Float), "valueToConvert");
        }

        internal static object Int(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(Int), args);

            if (ExpressionHelpers.TryConvertLong(args[0], out long value))
                return value;

            throw ArgumentInvalidInteger(nameof(Int), "valueToConvert");
        }

        internal static object Mod(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 2)
                throw ArgumentsOutOfRange(nameof(Mod), args);

            if (!ExpressionHelpers.TryConvertLong(args[0], out long operand1))
                throw ArgumentInvalidInteger(nameof(Mod), "operand1");

            if (!ExpressionHelpers.TryConvertLong(args[1], out long operand2))
                throw ArgumentInvalidInteger(nameof(Mod), "operand2");

            if (operand2 == 0)
                throw new DivideByZeroException();

            return operand1 % operand2;
        }

        internal static object Mul(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 2)
                throw ArgumentsOutOfRange(nameof(Mul), args);

            if (!ExpressionHelpers.TryConvertLong(args[0], out long operand1))
                throw ArgumentInvalidInteger(nameof(Mul), "operand1");

            if (!ExpressionHelpers.TryConvertLong(args[1], out long operand2))
                throw ArgumentInvalidInteger(nameof(Mul), "operand2");

            return operand1 * operand2;
        }

        internal static object Sub(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 2)
                throw ArgumentsOutOfRange(nameof(Sub), args);

            if (!ExpressionHelpers.TryConvertLong(args[0], out long operand1))
                throw ArgumentInvalidInteger(nameof(Sub), "operand1");

            if (!ExpressionHelpers.TryConvertLong(args[1], out long operand2))
                throw ArgumentInvalidInteger(nameof(Sub), "operand2");

            return operand1 - operand2;
        }

        #endregion Numeric

        #region Comparison

        /// <summary>
        /// equals(arg1, arg2)
        /// </summary>
        internal static object Equals(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 2)
                throw ArgumentsOutOfRange(nameof(Equals), args);

            // One null
            if (args[0] == null || args[1] == null)
                return args[0] == args[1];

            // Arrays
            if (args[0] is Array array1 && args[1] is Array array2)
                return SequenceEqual(array1, array2);
            else if (args[0] is Array || args[1] is Array)
                return false;

            // String and int
            if (ExpressionHelpers.TryString(args[0], out string s1) && ExpressionHelpers.TryString(args[1], out string s2))
                return s1 == s2;
            else if (ExpressionHelpers.TryString(args[0], out _) || ExpressionHelpers.TryString(args[1], out _))
                return false;
            else if (ExpressionHelpers.TryLong(args[0], out long i1) && ExpressionHelpers.TryLong(args[1], out long i2))
                return i1 == i2;
            else if (ExpressionHelpers.TryLong(args[0], out long _) || ExpressionHelpers.TryLong(args[1], out long _))
                return false;

            // Objects
            return ObjectEquals(args[0], args[1]);
        }

        /// <summary>
        /// greater(arg1, arg2)
        /// </summary>
        internal static object Greater(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 2)
                throw ArgumentsOutOfRange(nameof(Greater), args);

            return Compare(args[0], args[1]) > 0;
        }


        /// <summary>
        /// greaterOrEquals(arg1, arg2)
        /// </summary>
        internal static object GreaterOrEquals(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 2)
                throw ArgumentsOutOfRange(nameof(GreaterOrEquals), args);

            return Compare(args[0], args[1]) >= 0;
        }


        /// <summary>
        /// less(arg1, arg2)
        /// </summary>
        internal static object Less(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 2)
                throw ArgumentsOutOfRange(nameof(Less), args);

            return Compare(args[0], args[1]) < 0;
        }

        /// <summary>
        /// lessOrEquals(arg1, arg2)
        /// </summary>
        internal static object LessOrEquals(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 2)
                throw ArgumentsOutOfRange(nameof(LessOrEquals), args);

            return Compare(args[0], args[1]) <= 0;
        }

        #endregion Comparison

        #region Date

        internal static object DateTimeAdd(ITemplateContext context, object[] args)
        {
            var argCount = CountArgs(args);
            if (argCount < 2 || argCount > 3)
                throw ArgumentsOutOfRange(nameof(DateTimeAdd), args);

            if (!ExpressionHelpers.TryConvertDateTime(args[0], out DateTime startTime))
                throw ArgumentInvalidString(nameof(DateTimeAdd), "base");

            if (!ExpressionHelpers.TryString(args[1], out string duration))
                throw ArgumentInvalidString(nameof(DateTimeAdd), nameof(duration));

            string format = null;
            if (argCount == 3 && !ExpressionHelpers.TryString(args[2], out format))
                throw ArgumentInvalidString(nameof(DateTimeAdd), nameof(format));

            var timeToAdd = XmlConvert.ToTimeSpan(duration);
            var result = startTime.Add(timeToAdd);
            return format == null ? result.ToString(AzureCulture) : result.ToString(format, AzureCulture);
        }

        internal static object UtcNow(ITemplateContext context, object[] args)
        {
            var argCount = CountArgs(args);
            if (CountArgs(args) > 1)
                throw ArgumentsOutOfRange(nameof(UtcNow), args);

            var format = "yyyyMMddTHHmmssZ";
            if (argCount == 1 && !ExpressionHelpers.TryString(args[0], out format))
                throw ArgumentInvalidString(nameof(UtcNow), nameof(format));

            return DateTime.UtcNow.ToString(format, AzureCulture);
        }

        #endregion Date

        #region Logical

        /// <summary>
        /// and(arg1, arg2, ...)
        /// </summary>
        internal static object And(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length < 2)
                throw ArgumentsOutOfRange(nameof(And), args);

            for (var i = 0; i < args.Length; i++)
            {
                var expression = GetExpression(context, args[i]);
                if (!ExpressionHelpers.TryBool(expression, out bool bValue) || !bValue)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// bool(arg1)
        /// </summary>
        internal static object Bool(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 1)
                throw ArgumentsOutOfRange(nameof(Bool), args);

            if (ExpressionHelpers.TryConvertBool(args[0], out bool value))
                return value;

            throw ArgumentFormatInvalid(nameof(Bool));
        }

        /// <summary>
        /// false()
        /// </summary>
        internal static object False(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) > 0)
                throw ArgumentsOutOfRange(nameof(False), args);

            return false;
        }

        /// <summary>
        /// if(condition, trueValue, falseValue)
        /// </summary>
        internal static object If(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 3)
                throw ArgumentsOutOfRange(nameof(If), args);

            var expression = GetExpression(context, args[0]);
            if (ExpressionHelpers.TryBool(expression, out bool condition))
                return condition ? GetExpression(context, args[1]) : GetExpression(context, args[2]);

            throw ArgumentFormatInvalid(nameof(If));
        }

        /// <summary>
        /// not(arg1)
        /// </summary>
        internal static object Not(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 1)
                throw ArgumentsOutOfRange(nameof(Not), args);

            if (!ExpressionHelpers.TryBool(args[0], out bool value))
                throw ArgumentInvalidBoolean(nameof(Not), "arg1");

            return !value;
        }

        /// <summary>
        /// or(arg1, arg2, ...)
        /// </summary>
        internal static object Or(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length < 2)
                throw ArgumentsOutOfRange(nameof(Or), args);

            for (var i = 0; i < args.Length; i++)
            {
                if (ExpressionHelpers.TryBool(args[i], out bool value) && value)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// true()
        /// </summary>
        internal static object True(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) > 0)
                throw ArgumentsOutOfRange(nameof(True), args);

            return true;
        }

        #endregion Logical

        #region String

        internal static object Base64(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(Base64), args);

            if (!ExpressionHelpers.TryString(args[0], out string inputString))
                throw ArgumentInvalidString(nameof(Base64), nameof(inputString));

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(inputString));
        }

        internal static object Base64ToJson(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(Base64ToJson), args);

            if (!ExpressionHelpers.TryString(args[0], out string base64Value))
                throw ArgumentInvalidString(nameof(Base64ToJson), nameof(base64Value));

            return JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(base64Value)));
        }

        internal static object Base64ToString(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(Base64ToString), args);

            if (!ExpressionHelpers.TryString(args[0], out string base64Value))
                throw ArgumentInvalidString(nameof(Base64ToString), nameof(base64Value));

            return Encoding.UTF8.GetString(Convert.FromBase64String(base64Value));
        }

        internal static object DataUri(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(DataUri), args);

            if (!ExpressionHelpers.TryString(args[0], out string value))
                throw new ArgumentException();

            return string.Concat("data:text/plain;charset=utf8;base64,", Convert.ToBase64String(Encoding.UTF8.GetBytes(value)));
        }

        internal static object DataUriToString(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(DataUriToString), args);

            if (!ExpressionHelpers.TryString(args[0], out string value))
                throw new ArgumentException();

            var scheme = value.Substring(0, 5);
            if (scheme != "data:")
                throw new ArgumentException();

            var dataStart = value.IndexOf(',');
            var mediaType = value.Substring(5, dataStart - 5);
            var base64 = false;
            if (mediaType.EndsWith(";base64", ignoreCase: true, culture: AzureCulture))
            {
                base64 = true;
                mediaType = mediaType.Remove(mediaType.Length - 7);
            }
            var encoding = Encoding.UTF8;
            var data = value.Substring(dataStart + 1);
            return base64 ? encoding.GetString(Convert.FromBase64String(data)) : data;
        }

        internal static object EndsWith(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 2 || !ExpressionHelpers.TryString(args[0], out string s1) || !ExpressionHelpers.TryString(args[1], out string s2))
                throw new ArgumentOutOfRangeException();

            return s1.EndsWith(s2, StringComparison.OrdinalIgnoreCase);
        }

        internal static object Format(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) < 2)
                throw ArgumentsOutOfRange(nameof(Format), args);

            if (!ExpressionHelpers.TryString(args[0], out string formatString))
                throw new ArgumentException();

            var remaining = new object[args.Length - 1];
            System.Array.Copy(args, 1, remaining, 0, remaining.Length);
            return string.Format(AzureCulture, formatString, remaining);
        }

        internal static object Guid(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) < 1)
                throw ArgumentsOutOfRange(nameof(Guid), args);

            var hash = ExpressionHelpers.GetUnique(args);
            var guidBytes = new byte[16];
            System.Array.Copy(hash, 0, guidBytes, 0, 16);
            return new Guid(guidBytes).ToString();
        }

        internal static object IndexOf(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 2)
                throw ArgumentsOutOfRange(nameof(IndexOf), args);

            if (!ExpressionHelpers.TryString(args[0], out string stringToSearch))
                throw new ArgumentException();

            if (!ExpressionHelpers.TryString(args[1], out string stringToFind))
                throw new ArgumentException();

            return (long)stringToSearch.IndexOf(stringToFind, StringComparison.OrdinalIgnoreCase);
        }

        internal static object LastIndexOf(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 2)
                throw ArgumentsOutOfRange(nameof(LastIndexOf), args);

            if (!ExpressionHelpers.TryString(args[0], out string stringToSearch))
                throw new ArgumentException();

            if (!ExpressionHelpers.TryString(args[1], out string stringToFind))
                throw new ArgumentException();

            return (long)stringToSearch.LastIndexOf(stringToFind, StringComparison.OrdinalIgnoreCase);
        }

        internal static object NewGuid(ITemplateContext context, object[] args)
        {
            if (!(args == null || args.Length == 0))
                throw ArgumentsOutOfRange(nameof(NewGuid), args);

            return System.Guid.NewGuid().ToString();
        }

        internal static object PadLeft(ITemplateContext context, object[] args)
        {
            var argCount = CountArgs(args);
            if (argCount < 2 || argCount > 3)
                throw ArgumentsOutOfRange(nameof(PadLeft), args);

            string paddingCharacter = " ";

            if (!ExpressionHelpers.TryInt(args[1], out int totalLength))
                throw ArgumentInvalidInteger(nameof(PadLeft), "totalLength");

            if (argCount == 3 && (!ExpressionHelpers.TryString(args[2], out paddingCharacter) || paddingCharacter.Length > 1))
                throw new ArgumentException();

            if (ExpressionHelpers.TryString(args[0], out string svalue))
                return svalue.PadLeft(totalLength, paddingCharacter[0]);
            else if (ExpressionHelpers.TryInt(args[1], out int ivalue))
                return ivalue.ToString(new CultureInfo("en-us")).PadLeft(totalLength, paddingCharacter[0]);

            throw new ArgumentException();
        }

        internal static object StartsWith(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 2 || !ExpressionHelpers.TryString(args[0], out string s1) || !ExpressionHelpers.TryString(args[1], out string s2))
                throw new ArgumentOutOfRangeException();

            return s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase);
        }

        internal static object String(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 1)
                throw ArgumentsOutOfRange(nameof(String), args);

            if (ExpressionHelpers.TryBoolString(args[0], out string value))
                return value;

            return JsonConvert.SerializeObject(args[0]);
        }

        internal static object Substring(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length < 1 || args.Length > 3 || !ExpressionHelpers.TryString(args[0], out string value))
                throw ArgumentsOutOfRange(nameof(Substring), args);

            if (args.Length == 2 && ExpressionHelpers.TryInt(args[1], out int startIndex))
                return value.Substring(startIndex);
            else if (args.Length == 3 && ExpressionHelpers.TryInt(args[1], out int startIndex2) && ExpressionHelpers.TryInt(args[2], out int length))
                return value.Substring(startIndex2, length);

            throw ArgumentFormatInvalid(nameof(Substring));
        }

        internal static object ToLower(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(ToLower), args);

            if (args[0] is char c)
                return new string(char.ToLower(c, Thread.CurrentThread.CurrentCulture), 1);

            if (!ExpressionHelpers.TryString(args[0], out string stringToChange))
                throw new ArgumentException();

            return stringToChange.ToLower(AzureCulture);
        }

        internal static object ToUpper(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(ToUpper), args);

            if (args[0] is char c)
                return new string(char.ToUpper(c, Thread.CurrentThread.CurrentCulture), 1);

            if (!ExpressionHelpers.TryString(args[0], out string stringToChange))
                throw new ArgumentException();

            return stringToChange.ToUpper(AzureCulture);
        }

        internal static object Trim(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(Trim), args);

            if (!ExpressionHelpers.TryString(args[0], out string stringToTrim))
                throw new ArgumentException();

            return stringToTrim.Trim();
        }

        internal static object UniqueString(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) == 0)
                throw ArgumentsOutOfRange(nameof(UniqueString), args);

            return ExpressionHelpers.GetUniqueString(args).Substring(0, 13);
        }

        internal static object Uri(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 2)
                throw ArgumentsOutOfRange(nameof(Uri), args);

            if (!ExpressionHelpers.TryString(args[0], out string baseUri))
                throw ArgumentInvalidString(nameof(Uri), "baseUri");

            if (!ExpressionHelpers.TryString(args[1], out string relativeUri))
                throw ArgumentInvalidString(nameof(Uri), "relativeUri");

            var result = new Uri(new Uri(baseUri), relativeUri);
            return result.ToString();
        }

        internal static object UriComponent(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(UriComponent), args);

            if (!ExpressionHelpers.TryString(args[0], out string stringToEncode))
                throw ArgumentInvalidString(nameof(UriComponent), "stringToEncode");

            return HttpUtility.UrlEncode(stringToEncode);
        }

        internal static object UriComponentToString(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 1)
                throw ArgumentsOutOfRange(nameof(UriComponentToString), args);

            if (!ExpressionHelpers.TryString(args[0], out string uriEncodedString))
                throw ArgumentInvalidString(nameof(UriComponentToString), "uriEncodedString");

            return HttpUtility.UrlDecode(uriEncodedString);
        }

        internal static object Replace(ITemplateContext context, object[] args)
        {
            if (CountArgs(args) != 3)
                throw ArgumentsOutOfRange(nameof(Replace), args);

            if (!ExpressionHelpers.TryString(args[0], out string originalString) || !ExpressionHelpers.TryString(args[1], out string oldString) || !ExpressionHelpers.TryString(args[2], out string newString))
                throw new ArgumentException();

            return originalString.Replace(oldString, newString);
        }

        internal static object Split(ITemplateContext context, object[] args)
        {
            if (args == null || args.Length != 2 || !ExpressionHelpers.TryString(args[0], out string value))
                throw new ArgumentOutOfRangeException();

            string[] delimiter = null;
            if (ExpressionHelpers.TryString(args[1], out string single))
            {
                delimiter = new string[] { single };
            }
            else if (args[1] is Array delimiters)
            {
                delimiter = new string[delimiters.Length];
                delimiters.CopyTo(delimiter, 0);
            }
            else if (TryJArray(args[1], out JArray jArray))
            {
                delimiter = jArray.Values<string>().ToArray();
            }
            else
                throw new ArgumentException();

            return new JArray(value.Split(delimiter, StringSplitOptions.None));
        }

        #endregion String

        #region Helper functions

        private static int Compare(object left, object right)
        {
            if (ExpressionHelpers.TryLong(left, out long longLeft) && ExpressionHelpers.TryLong(right, out long longRight))
                return Comparer<long>.Default.Compare(longLeft, longRight);
            else if (ExpressionHelpers.TryString(left, out string stringLeft) && ExpressionHelpers.TryString(right, out string stringRight))
                return StringComparer.Ordinal.Compare(stringLeft, stringRight);

            return Comparer.Default.Compare(left, right);
        }

        private static bool SequenceEqual(Array array1, Array array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (var i = 0; i < array1.Length; i++)
            {
                if (array1.GetValue(i) != array2.GetValue(i))
                    return false;
            }
            return true;
        }

        private static bool ObjectEquals(object o1, object o2)
        {
            var objectType = o1.GetType();
            if (objectType != o2.GetType())
                return false;

            var props = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
            for (var i = 0; i < props.Length; i++)
            {
                if (!object.Equals(props[i].GetValue(o1), props[i].GetValue(o2)))
                    return false;
            }
            return true;
        }

        private static bool IsNull(object o)
        {
            return o == null || (o is JToken token && token.Type == JTokenType.Null);
        }

        private static bool IsString(object o)
        {
            return o is string || (o is JToken token && token.Type == JTokenType.String);
        }

        private static bool TryJArray(object o, out JArray value)
        {
            value = null;
            if (o is JArray jArray)
            {
                value = jArray;
                return true;
            }
            return false;
        }

        private static bool TryJObject(object o, out JObject value)
        {
            value = null;
            if (o is JObject jObject)
            {
                value = jObject;
                return true;
            }
            return false;
        }

        private static bool Contains(Array array, object o)
        {
            var objectToFind = o;
            if (objectToFind is JToken jToken)
                objectToFind = jToken.Value<object>();

            for (var i = 0; i < array.Length; i++)
            {
                if (ObjectEquals(array.GetValue(i), objectToFind))
                    return true;
            }
            return false;
        }

        private static int CountArgs(object[] args)
        {
            return args == null ? 0 : args.Length;
        }

        private static string TrimResourceType(string resourceType)
        {
            return resourceType[resourceType.Length - 1] == '/' ? resourceType = resourceType.Substring(0, resourceType.Length - 1) : resourceType;
        }

        private static object GetExpression(ITemplateContext context, object o)
        {
            return o is ExpressionFnOuter fn ? fn(context) : o;
        }

        #endregion Helper functions

        #region Exceptions

        private static ExpressionArgumentException ArgumentsOutOfRange(string expression, object[] args)
        {
            var length = args == null ? 0 : args.Length;
            return new ExpressionArgumentException(
                expression,
                string.Format(Thread.CurrentThread.CurrentCulture, PSRuleResources.ArgumentsOutOfRange, expression, length)
            );
        }

        private static ExpressionArgumentException ArgumentFormatInvalid(string expression)
        {
            return new ExpressionArgumentException(
                expression,
                string.Format(Thread.CurrentThread.CurrentCulture, PSRuleResources.ArgumentFormatInvalid, expression)
            );
        }

        private static ExpressionArgumentException ArgumentInvalidInteger(string expression, string operand)
        {
            return new ExpressionArgumentException(
                expression,
                string.Format(Thread.CurrentThread.CurrentCulture, PSRuleResources.ArgumentInvalidInteger, operand, expression)
            );
        }

        private static ExpressionArgumentException ArgumentInvalidBoolean(string expression, string operand)
        {
            return new ExpressionArgumentException(
                expression,
                string.Format(Thread.CurrentThread.CurrentCulture, PSRuleResources.ArgumentInvalidBoolean, operand, expression)
            );
        }

        private static ExpressionArgumentException ArgumentInvalidString(string expression, string operand)
        {
            return new ExpressionArgumentException(
                expression,
                string.Format(Thread.CurrentThread.CurrentCulture, PSRuleResources.ArgumentInvalidString, operand, expression)
            );
        }

        private static ExpressionArgumentException ArgumentInvalidResourceType(string expression, string providerNamespace, string resourceType)
        {
            return new ExpressionArgumentException(
                expression,
                string.Format(Thread.CurrentThread.CurrentCulture, PSRuleResources.ArgumentInvalidResourceType, expression, providerNamespace, resourceType)
            );
        }

        #endregion Exceptions
    }
}
