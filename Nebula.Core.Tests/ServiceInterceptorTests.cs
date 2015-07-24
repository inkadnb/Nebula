﻿using Castle.DynamicProxy;
using Nebula.Core.Tests.TestConstructs;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Core.Tests
{
    public class ServiceInterceptorTests
    {
        [TestFixture]
        public class Intercept
        {
            [TestCase]
            public void TestInterception()
            {
                ServiceRegistry.Instance.Register<IBogusServiceInterface, BogusService>();

                var session = new Session();

                var bogusTrackable = new BogusTrackable();
                bogusTrackable.Test = "Test!";

                session.WorkingSet.Add(bogusTrackable);

                var service = ServiceProxyGenerator.GetService<IBogusServiceInterface>(session);

                var expected = "Bogus!";

                var result = service.ReturnBogusString();

                Assert.True(result == expected);
            }
        }
    }
}
