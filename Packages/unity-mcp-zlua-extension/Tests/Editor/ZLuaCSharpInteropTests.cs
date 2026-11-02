using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityMcp.ZLua.Tests
{
    public sealed class ZLuaCSharpInteropTests
    {
        private const string Fixture = "CSharp['UnityMcp.ZLua.Editor.Tests']['UnityMcp.ZLua.Tests.InteropFixture']";

        [Test]
        public void LuaReadsUnityStaticProperty()
        {
            using (var host = new ZLuaScriptHost("interop-test"))
            {
                var result = host.Eval("return CSharp['UnityEngine.CoreModule']['UnityEngine.Application'].unityVersion");
                StringAssert.Contains(Application.unityVersion, result);
            }
        }

        [Test]
        public void LuaCallsCSharpStaticMethod()
        {
            using (var host = new ZLuaScriptHost("interop-test"))
            {
                Assert.AreEqual("{\"kind\":\"number\",\"value\":7}", host.Eval("return " + Fixture + ".Add(2, 5)"));
            }
        }

        [Test]
        public void LuaConstructsObjectAndUsesFieldPropertyAndInstanceMethod()
        {
            using (var host = new ZLuaScriptHost("interop-test"))
            {
                var result = host.Eval("local item = " + Fixture + "(); item.Value = 12; item.Name = 'bridge'; item:Increment(5); return { value = item.Value, name = item.Name }");
                StringAssert.Contains("\"value\":17", result);
                StringAssert.Contains("\"name\":\"bridge\"", result);
            }
        }

        [Test]
        public void LuaCallsUnityGameObjectInstanceMethod()
        {
            var gameObject = new GameObject("zlua-interop-test");
            InteropFixture.Target = gameObject;
            try
            {
                using (var host = new ZLuaScriptHost("interop-test"))
                {
                    var result = host.Eval("local target = " + Fixture + ".Target; target:SetActive(false); return target.activeSelf");
                    Assert.AreEqual("{\"kind\":\"boolean\",\"value\":false}", result);
                    Assert.False(gameObject.activeSelf);
                }
            }
            finally
            {
                InteropFixture.Target = null;
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MissingCSharpMemberReturnsAnError()
        {
            using (var host = new ZLuaScriptHost("interop-test"))
            {
                Assert.That(() => host.Eval("return " + Fixture + ".DoesNotExist"), Throws.Exception);
            }
        }
    }

    public sealed class InteropFixture
    {
        public int Value;
        public string Name { get; set; }
        public static GameObject Target;

        public static int Add(int left, int right)
        {
            return left + right;
        }

        public void Increment(int amount)
        {
            Value += amount;
        }
    }
}
