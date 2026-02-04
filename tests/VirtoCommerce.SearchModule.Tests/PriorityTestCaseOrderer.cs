using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.v3;

namespace VirtoCommerce.SearchModule.Tests
{
    public class PriorityTestCaseOrderer : ITestCaseOrderer
    {
        IReadOnlyCollection<TTestCase> ITestCaseOrderer.OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases)
        {
            return testCases.OrderByDescending(tc => GetPriority((IXunitTestCase)tc)).ToList();
        }

        private static int GetPriority(IXunitTestCase testCase)
        {
            // Order the test based on the attribute.
            var type = FindType(testCase.TestClassName);
            var methodInfo = type?.GetMethod(testCase.TestMethodName);
            var attr = methodInfo?.GetCustomAttribute<PriorityAttribute>();

            return attr?.Priority ?? 0;
        }

        private static Type FindType(string typeName)
        {
            // First try direct lookup (works for current assembly and mscorlib)
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            // Search all loaded assemblies for cross-assembly test classes
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
