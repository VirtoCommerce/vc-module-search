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
            var type = Type.GetType(testCase.TestClassName);
            var methodInfo = type?.GetMethod(testCase.TestMethodName);
            var attr = methodInfo?.GetCustomAttribute<PriorityAttribute>();

            return attr?.Priority ?? 0;
        }
    }
}
