﻿using System;
using System.Reflection;
using UnityEngine;
using NUnit.Framework;
using SceneTest;

namespace UnityIoC.Editor
{
    public class AssemblyContextTests : TestBase
    {
        [Test]
        public void t1_get_default_instance()
        {
            Assert.IsNotNull(Context.DefaultInstance);
        }


        [Test]
        public void t2_bind_from_tests_setting()
        {
            var obj = Context.ResolveObject<TestInterface>();
            Assert.IsNotNull(obj);
        }


        [Test]
        public void t3_bind_types()
        {
            //TestClass implements the TestInterface
            Context.Bind<TestInterface, TestClass>();
            var obj = Context.ResolveObject<TestInterface>();
            Assert.IsNotNull(obj);
        }


        [Test]
        public void t4_bind_instance()
        {
            var instance = new TestClass();
            Context.Bind(instance);
            var obj = Context.ResolveObject<TestInterface>();
            Assert.IsNotNull(obj);
        }


        [Test]
        public void t5_bind_as()
        {
            var instance = Context.ResolveObject<TestClass>(LifeCycle.Singleton);
            //assign singleton value to
            var testString = "Hello";
            instance.JustAProperty = testString;

            //try to resolve as transient, this is a brand new object
            var transient = Context.ResolveObject<TestClass>(LifeCycle.Transient);

            //so the value of the property should be as default
            Assert.IsNull(transient.JustAProperty);

            //now try to resolve as singleton, this should be the previous object which have been created
            var singleton = Context.ResolveObject<TestClass>(LifeCycle.Singleton);

            //verify by the property's value
            Assert.AreSame(singleton, instance);
            Assert.AreEqual(singleton.JustAProperty, testString);
        }


        [Test]
        public void t6_load_custom_setting()
        {
            Context.Bind(Resources.Load<InjectIntoBindingSetting>("not_default"));
            var obj = Context.ResolveObject<TestInterface>();
            Assert.IsNotNull(obj);
        }


        [Test]
        public void t7_resolve_unregistered_objects()
        {
            var obj = Context.ResolveObject<TestClass>();
            Assert.IsNotNull(obj);

            var assem = Assembly.Load("Tests");
            var type = assem.GetType("TestInterface");
            Assert.IsNotNull(type);
        }

        //you can enable this test if running on Unity 2018.3 or newer-
        
#if UNITY_2018_3_OR_NEWER
        [Test]
        public void t8_instantiate_prefabs()
        {
            //create a context which processes the SceneTest assembly
            Context.GetDefaultInstance(typeof(TestComponent));

            //create a gameobject with TestComponent as a prefab
            var prefab = Context.ResolveObject<TestComponent>();
            prefab.gameObject.SetActive(false);

            //Instantiate the prefab (a.k.a clone)
            var instance = Context.Instantiate(prefab);
            instance.gameObject.SetActive(true);

            Assert.IsNotNull(instance);
            Assert.IsNotNull(instance.abstractClass);
        }
#endif
        [Test]
        public void t9_resolve_actions()
        {
            Action<TestInterface> action = t =>
            {
                Assert.IsNotNull(t);
                t.DoSomething();
            };
            Context.GetDefaultInstance(this).ResolveAction(action);
        }

        [Test]
        public void t10_resolve_funcs()
        {
            var obj = Context.ResolveObject<TestInterface>(LifeCycle.Singleton);
            obj.JustAProperty = "Hello";

            Func<TestInterface, string> func = t =>
            {
                Assert.IsNotNull(t);
                return t.JustAProperty;
            };

            var func_output = Context.GetDefaultInstance(this).ResolveFunc(func, LifeCycle.Singleton);
            Assert.AreEqual(func_output, "Hello");
        }


        [Test]
        public void t11_dispose_instance()
        {
            Assert.IsFalse(Context.Initialized);
//create
            Context.GetDefaultInstance(this);

            Assert.IsTrue(Context.Initialized);
//dispose
            Context.DisposeDefaultInstance();
//assert
            Assert.IsFalse(Context.Initialized);
        }

        [TearDown]
        public void Dispose()
        {
            Context.DisposeDefaultInstance();
        }
    }
}