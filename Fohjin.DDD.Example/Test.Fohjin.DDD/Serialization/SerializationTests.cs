﻿using Fohjin.DDD.Events.Account;
using Fohjin.DDD.EventStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Test.Fohjin.DDD.TestUtilities;

namespace Test.Fohjin.DDD.Serialization
{
    [TestClass]
    public class SerializationTests
    {
        public TestContext TestContext { get; set; }


        [DataTestMethod]
        [DynamicData(nameof(TestData), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestDataDisplayName))]
        public void ModelPersistenceTest(Type type)
        {
            var obj = type.BuildObject();
            TestContext
                .AddResults(type.Name, obj)
                .GetResults(type.Name, type, out var result)
                .AddResults(type.Name + "_back", result)
                ;
            type.EnsureNotDefault(obj);
            Assert.IsNotNull(result);
        }
        public static string TestDataDisplayName(MethodInfo methodInfo, object[] data) =>
            $"{methodInfo.Name} for {((Type)data[0]).Name}";

        public static IEnumerable<object[]> TestData()
        {
            var commands = typeof(ICommand).GetInstanceTypes();
            var domainEvents = typeof(IDomainEvent).GetInstanceTypes();

            var items = commands.Concat(domainEvents);
            var mapped = items.Select(i => new object[] { i });
            return mapped;
        }
    }
}
