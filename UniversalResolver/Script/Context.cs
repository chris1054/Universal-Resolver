﻿/**
 * Author:    Vinh Vu Thanh
 * This class is a part of Universal Resolver project that can be downloaded free at 
 * https://github.com/game-libgdx-unity/UnityEngine.IoC
 * (c) Copyright by MrThanhVinh168@gmail.com
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityIoC
{
    public partial class Context
    {
        private const string DefaultAssemblyName = "Assembly-CSharp";

        #region Variables & Constants

        private readonly Logger debug = new Logger(typeof(Context));

        //cache inject attributes
        private List<InjectAttribute> injectAttributes = new List<InjectAttribute>();

        //cache objects from scene
        private Dictionary<Type, MonoBehaviour> monoScripts = new Dictionary<Type, MonoBehaviour>();

        public bool automaticBinding = false;

        public Type TargetType { get; set; }

        private Container container;

        private bool initialized;

        private string assemblyName = DefaultAssemblyName;

        #endregion

        #region Constructors

        /// <summary>
        /// Pass a object belong to the assembly you want to process
        /// </summary>
        /// <param name="target">the object</param>
        /// <param name="autoFindBindSetting">if true, will load bindingsetting and process all game object for inject attribute</param>
        public Context(object target, bool autoFindBindSetting = true)
        {
            Initialize(target.GetType(), autoFindBindSetting);
        }

        /// <summary>
        /// Pass a type belong to the assembly you want to process
        /// </summary>
        /// <param name="target">the object</param>
        /// <param name="autoFindBindSetting">if true, will load bindingsetting and process all game object for inject attribute</param>
        public Context(Type typeInTargetedAssembly, bool autoFindBindSetting = true)
        {
            Initialize(typeInTargetedAssembly, autoFindBindSetting);
        }


        /// <summary>
        /// Use the default assembly to process
        /// </summary>
        /// <param name="target">the object</param>
        /// <param name="autoFindBindSetting">if true, will load bindingsetting and process all game object for inject attribute</param>
        public Context()
        {
            Initialize(null, false);
        }

        #endregion

        #region Private members

        /// <summary>
        /// Process automatically bindingSetting, then process inject attribute in every game object
        /// </summary>
        private void InitialProcess()
        {
            if (container == null)
            {
                debug.LogError("You need to call Initialize before calling this method");
                return;
            }

            if (!automaticBinding)
            {
                return;
            }

            debug.Log("Processing assembly {0}...", CurrentAssembly.GetName().Name);

            //try to load a default InjectIntoBindingSetting setting for context
            var injectIntoBindingSetting =
                UnityEngine.Resources.Load<InjectIntoBindingSetting>(CurrentAssembly.GetName().Name);

            if (injectIntoBindingSetting)
            {
                debug.Log("Found InjectIntoBindingSetting for assembly");
                LoadBindingSetting(injectIntoBindingSetting);
            }

            //try to get the default InjectIntoBindingSetting setting for current scene
            var sceneInjectIntoBindingSetting = UnityEngine.Resources.Load<InjectIntoBindingSetting>(
                string.Format("{0}_{1}", CurrentAssembly.GetName().Name, SceneManager.GetActiveScene().name)
            );

            if (sceneInjectIntoBindingSetting)
            {
                debug.Log("Found InjectIntoBindingSetting for scene");
                LoadBindingSetting(sceneInjectIntoBindingSetting);
            }

            //try to load a default BindingSetting setting for context
            var bindingSetting =
                UnityEngine.Resources.Load<BindingSetting>(CurrentAssembly.GetName().Name);

            if (bindingSetting)
            {
                debug.Log("Found binding setting for assembly");
                LoadBindingSetting(bindingSetting);
            }

            //try to get the default BindingSetting setting for current scene
            var sceneBindingSetting = UnityEngine.Resources.Load<BindingSetting>(
                string.Format("{0}_{1}", CurrentAssembly.GetName().Name, SceneManager.GetActiveScene().name)
            );

            if (sceneBindingSetting)
            {
                debug.Log("Found binding setting for scene");
                LoadBindingSetting(sceneBindingSetting);
            }


            ProcessInjectAttributeForMonoBehaviours();
        }

        /// <summary>
        /// Read binding data to create registerObjects
        /// </summary>
        /// <param name="bindingSetting"></param>
        /// <param name="InjectIntoType"></param>
        private void BindFromSetting(BindingAsset bindingSetting, Type InjectIntoType = null)
        {
            GameObject prefab = null;
            if (bindingSetting.AbstractType == null)
            {
                bindingSetting.AbstractType =
                    GetTypeFromCurrentAssembly(bindingSetting.AbstractTypeHolder.name);

                if (bindingSetting.AbstractType == null)
                {
                    debug.LogError("bindingSetting.AbstractType should not null!");
                    return;
                }
            }

            if (bindingSetting.ImplementedType == null)
            {
                if (bindingSetting.ImplementedTypeHolder is GameObject)
                {
                    prefab = bindingSetting.ImplementedTypeHolder as GameObject;
                    if (!bindingSetting.AbstractType.IsAbstract)
                    {
                        bindingSetting.ImplementedType = bindingSetting.AbstractType;
                    }
                    else
                    {
                        bindingSetting.ImplementedType = prefab.GetComponent(bindingSetting.AbstractType).GetType();
                    }
                }
                else
                {
                    bindingSetting.ImplementedType =
                        GetTypeFromCurrentAssembly(bindingSetting.ImplementedTypeHolder.name);
                }

                if (bindingSetting.ImplementedType == null)
                {
                    debug.LogError("bindingSetting.ImplementedType should not null!");
                    return;
                }
            }

            var lifeCycle = bindingSetting.LifeCycle;
            if (prefab != null)
            {
                lifeCycle = LifeCycle.Singleton | LifeCycle.Prefab;
            }


            debug.Log("Bind from setting {0} for {1} by {2} when inject into {3}",
                bindingSetting.ImplementedType,
                bindingSetting.AbstractType,
                lifeCycle.ToString(),
                InjectIntoType != null ? InjectIntoType.Name : "Null");


            //bind it with inject into type
            var injectIntoBindingSetting = new InjectIntoBindingData();

            injectIntoBindingSetting.Prefab = prefab;
            injectIntoBindingSetting.ImplementedType = bindingSetting.ImplementedType;
            injectIntoBindingSetting.AbstractType = bindingSetting.AbstractType;

            injectIntoBindingSetting.LifeCycle = lifeCycle;
            injectIntoBindingSetting.EnableInjectInto = InjectIntoType != null;
            injectIntoBindingSetting.InjectInto = InjectIntoType;

            defaultContainer.Bind(injectIntoBindingSetting);
        }

        /// <summary>
        /// Read binding data to create registerObjects
        /// </summary>
        /// <param name="bindingSetting"></param>
        private void BindFromSetting(InjectIntoBindingAsset bindingSetting)
        {
            GameObject prefab = null;
            if (bindingSetting.AbstractType == null)
            {
                bindingSetting.AbstractType =
                    GetTypeFromCurrentAssembly(bindingSetting.AbstractTypeHolder.name);

                if (bindingSetting.AbstractType == null)
                {
                    debug.LogError("bindingSetting.AbstractType should not null!");
                    return;
                }
            }

            if (bindingSetting.ImplementedType == null)
            {
                if (bindingSetting.ImplementedTypeHolder is GameObject)
                {
                    prefab = bindingSetting.ImplementedTypeHolder as GameObject;
                    if (!bindingSetting.AbstractType.IsAbstract)
                    {
                        bindingSetting.ImplementedType = bindingSetting.AbstractType;
                    }
                    else
                    {
                        bindingSetting.ImplementedType = prefab.GetComponent(bindingSetting.AbstractType).GetType();
                    }
                }
                else
                    bindingSetting.ImplementedType =
                        GetTypeFromCurrentAssembly(bindingSetting.ImplementedTypeHolder.name);

                if (bindingSetting.ImplementedType == null)
                {
                    debug.LogError("bindingSetting.ImplementedType should not null!");
                    return;
                }
            }

            //bind it with inject into type
            var injectIntoBindingSetting = new InjectIntoBindingData();
            injectIntoBindingSetting.Prefab = prefab;
            injectIntoBindingSetting.ImplementedType = bindingSetting.ImplementedType;
            injectIntoBindingSetting.AbstractType = bindingSetting.AbstractType;
            injectIntoBindingSetting.LifeCycle = bindingSetting.LifeCycle;
            injectIntoBindingSetting.EnableInjectInto =
                bindingSetting.EnableInjectInto && bindingSetting.InjectIntoHolder.Count > 0;

            //binding with inject into
            if (injectIntoBindingSetting.EnableInjectInto)
            {
                foreach (var injectIntoHolder in bindingSetting.InjectIntoHolder)
                {
                    if (injectIntoHolder != null)
                    {
                        var injectIntoType = GetTypeFromCurrentAssembly(injectIntoHolder.name);

                        if (injectIntoType != null)
                        {
                            debug.Log("Bind from setting {0} for {1} by {2} when injectInto {3}",
                                bindingSetting.ImplementedType,
                                bindingSetting.AbstractType,
                                bindingSetting.LifeCycle.ToString(),
                                injectIntoHolder.name);

                            injectIntoBindingSetting.InjectInto = injectIntoType;
                            defaultContainer.Bind(injectIntoBindingSetting);
                        }
                    }
                }
            }
            //binding without inject into
            else
            {
                defaultContainer.Bind(injectIntoBindingSetting);
            }
        }


        /// <summary>
        /// Get a type from an assembly by its name
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        private Type GetTypeFromCurrentAssembly(string className)
        {
            foreach (var type in CurrentAssembly.GetTypes())
            {
                if (type.Name == className)
                {
                    return type;
                }
            }

            debug.Log("Cannot get type {0} from assembly {1}", className, CurrentAssembly.GetName().Name);
            return null;
        }

        public void Dispose()
        {
            initialized = false;
            monoScripts.Clear();
            injectAttributes.Clear();
            TargetType = null;

            if (container != null)
            {
                container.Dispose();
            }
        }

        /// <summary>
        /// Process an object info for resolving any inject attribute from fields, properties, methods
        /// </summary>
        /// <param name="mono"></param>
        /// <param name="ignoreMonobehaviour"></param>
        private void ProcessInjectAttribute(object mono, bool ignoreMonobehaviour = false)
        {
            if (mono == null)
            {
                return;
            }

            var gameObj = mono as GameObject;
            if (gameObj)
            {
                foreach (var component in gameObj.GetComponents(typeof(MonoBehaviour)))
                {
                    ProcessMethod(component, ignoreMonobehaviour);
                    ProcessProperties(component, ignoreMonobehaviour);
                    ProcessVariables(component, ignoreMonobehaviour);
                }

                return;
            }

            var asObject = mono as Object;
            var asBehaviour = mono as MonoBehaviour;
            if (asObject == null || asBehaviour != null)
            {
                ProcessMethod(mono, ignoreMonobehaviour);
                ProcessProperties(mono, ignoreMonobehaviour);
                ProcessVariables(mono, ignoreMonobehaviour);
            }
        }

        /// <summary>
        /// Process a method for resolving any inject attribute
        /// </summary>
        /// <param name="mono"></param>
        /// <param name="ignoreMonobehaviour"></param>
        private void ProcessMethod(object mono, bool ignoreMonobehaviour)
        {
            Type objectType = mono.GetType();
            var methods = objectType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(method => method.IsDefined(typeof(InjectAttribute), true))
                .ToArray();

            if (methods.Length <= 0) return;

            debug.Log(string.Format("Found {0} method to process", methods.Length));

            foreach (var method in methods)
            {
                ProcessMethodInfo(mono, method, ignoreMonobehaviour);
            }
        }

        /// <summary>
        /// Process a method info for resolving any inject attribute
        /// </summary>
        /// <param name="mono"></param>
        /// <param name="method"></param>
        /// <param name="ignoreMonobehaviour"></param>
        /// <param name="inject"></param>
        private void ProcessMethodInfo(object mono, MethodInfo method, bool ignoreMonobehaviour,
            InjectAttribute inject = null)
        {
            if (inject == null)
            {
                inject = method.GetCustomAttributes(typeof(InjectAttribute), true).FirstOrDefault() as InjectAttribute;
            }

            injectAttributes.Add(inject);

            var parameters = method.GetParameters();

            //ignore process for monobehaviour
            if (ignoreMonobehaviour)
            {
                if (parameters.Any(p => p.ParameterType.IsSubclassOf(typeof(Component))))
                {
                    return;
                }
            }

            var paramObjects = Array.ConvertAll(parameters, p =>
            {
                debug.Log("Para: " + p.Name + " " + p.ParameterType);
                return container.ResolveObject(p.ParameterType,
                    inject == null ? LifeCycle.Default : inject.LifeCycle, mono);
            });
            method.Invoke(mono, paramObjects);
        }

        /// <summary>
        /// Process a property info for resolving any inject attribute
        /// </summary>
        /// <param name="mono"></param>
        /// <param name="ignoreMonobehaviour"></param>
        private void ProcessProperties(object mono, bool ignoreMonobehaviour)
        {
            Type objectType = mono.GetType();
            var properties = objectType
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(property => property.IsDefined(typeof(InjectAttribute), false))
                .ToArray();

            if (properties.Length <= 0) return;

            debug.Log(string.Format("Found {0} property to process", properties.Length));

            foreach (var property in properties)
            {
                if (ignoreMonobehaviour)
                {
                    if (property.PropertyType.IsSubclassOf(typeof(Component)))
                    {
                        continue;
                    }
                }

                var inject = property
                    .GetCustomAttributes(typeof(InjectAttribute), true)
                    .FirstOrDefault() as InjectAttribute;

                if (inject == null)
                {
                    continue;
                }

                //only process a property if the property's value is not set yet
                var value = property.GetValue(mono, null);
                if (value != value.DefaultValue())
                {
                    debug.Log(string.Format("Don't set value for property {0} due to non-default value",
                        property.Name));

                    //check to bind this instance
                    if (inject.LifeCycle == LifeCycle.Singleton ||
                        (inject.LifeCycle & LifeCycle.Singleton) == LifeCycle.Singleton)
                    {
                        container.Bind(property.PropertyType, value);
                    }

                    continue;
                }

                var setMethod = property.GetSetMethod(true);

                //pass container to injectAttribute
                // ReSharper disable once PossibleNullReferenceException
                inject.container = DefaultContainer;

                //resolve as [Component] attributes
                //try to resolve as monoBehaviour or as array
                if (property.PropertyType.IsArray)
                {
                    var components = GetComponentsFromGameObject(mono, property.PropertyType, inject);
                    if (components != null && components.Length > 0)
                    {
                        property.SetValue(mono,
                            ConvertComponentArrayTo(property.PropertyType.GetElementType(), components), null);
                        continue;
                    }
                }
                else
                {
                    var component = GetComponentFromGameObject(mono, property.PropertyType, inject);
                    if (component)
                    {
                        if (inject.LifeCycle == LifeCycle.Singleton ||
                            (inject.LifeCycle & LifeCycle.Singleton) == LifeCycle.Singleton)
                        {
                            container.Bind(property.PropertyType, component);
                        }

                        property.SetValue(mono, component, null);
                        continue;
                    }
                }

                debug.Log("IComponentResolvable attribute fails to resolve {0}", property.PropertyType);
                //resolve object as [Singleton], [Transient] or [AsComponent] if component attribute fails to resolve
                ProcessMethodInfo(mono, setMethod, ignoreMonobehaviour, inject);
            }
        }

        private void ProcessVariables(object mono, bool ignoreMonobehaviour)
        {
            Type objectType = mono.GetType();
            var fieldInfos = objectType
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(fieldInfo => fieldInfo.IsDefined(typeof(InjectAttribute), true))
                .ToArray();

            if (fieldInfos.Length > 0)
                debug.Log(string.Format("Found {0} fieldInfo to process", fieldInfos.Length));

            foreach (var field in fieldInfos)
            {
                if (ignoreMonobehaviour)
                {
                    if (field.FieldType.IsSubclassOf(typeof(Component)))
                    {
                        continue;
                    }
                }

                var inject =
                    field.GetCustomAttributes(typeof(InjectAttribute), true).FirstOrDefault() as InjectAttribute;

                if (inject == null)
                {
                    continue;
                }

                //only process a field if the field's value is not set yet
                var value = field.GetValue(mono);
                if (value != value.DefaultValue())
                {
                    debug.Log(string.Format("Don't set value for field {0} due to non-default value", field.Name));

                    //check to bind this instance
                    if (inject.LifeCycle == LifeCycle.Singleton ||
                        (inject.LifeCycle & LifeCycle.Singleton) == LifeCycle.Singleton)
                    {
                        debug.Log(string.Format("Bind instance for field {0}", field.Name));
                        container.Bind(field.FieldType, value);
                    }

                    continue;
                }

                injectAttributes.Add(inject);

                //pass container to injectAttribute
                inject.container = DefaultContainer;

                if (field.FieldType.IsArray)
                {
                    //check if field type is array for particular processing
                    var injectComponentArray = inject as IComponentArrayResolvable;

                    if (injectComponentArray == null)
                    {
                        throw new InvalidOperationException(
                            "You must apply injectAttribute implementing IComponentArrayResolvable field to resolve the array of components");
                    }

                    //try to resolve as monoBehaviour
                    var components = GetComponentsFromGameObject(mono, field.FieldType, inject);
                    if (components != null && components.Length > 0)
                    {
                        var array = ConvertComponentArrayTo(field.FieldType.GetElementType(), components);
                        field.SetValue(mono, array);
                        continue;
                    }
                }
                else
                {
                    //try to resolve as monoBehaviour component
                    var component = GetComponentFromGameObject(mono, field.FieldType, inject);
                    if (component)
                    {
                        //if the life cycle is singleton, bind the instance of the Type with this component
                        if (inject.LifeCycle == LifeCycle.Singleton ||
                            (inject.LifeCycle & LifeCycle.Singleton) == LifeCycle.Singleton)
                        {
                            container.Bind(field.FieldType, component);
                        }

                        field.SetValue(mono, component);

                        continue;
                    }
                }


                debug.Log("IComponentResolvable attribute fails to resolve {0}", field.FieldType);

                //resolve object as [Singleton], [Transient] or [AsComponent] if component attribute fails to resolve
                field.SetValue(mono,
                    container.ResolveObject(
                        field.FieldType,
                        inject.LifeCycle,
                        mono));
            }
        }

        private Array ConvertComponentArrayTo(Type typeOfArray, Component[] components)
        {
            var array = Array.CreateInstance(typeOfArray, components.Length);
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];

                if (component)
                {
                    array.SetValue(component, i);
                }
            }

            return array;
        }

        /// <summary>
        /// Try to resolve unity component, this should be used in other attribute process methods
        /// </summary>
        /// <param name="mono">object is expected as unity mono behaviour</param>
        /// <returns>the component</returns>
        private Component GetComponentFromGameObject(object mono, Type type, InjectAttribute injectAttribute)
        {
            var behaviour = mono as MonoBehaviour;

            if (behaviour == null) return null;

            //output component
            Component component = null;

            //resolve by inject component to the gameobject
            var injectComponent = injectAttribute as IComponentResolvable;

            //try get/add component with IInjectComponent interface
            if (injectComponent != null)
            {
                component = injectComponent.GetComponent(behaviour, type);
                //unable to get it from gameObject
                if (component != null)
                {
                    ProcessInjectAttribute(component);

                    if (injectAttribute.LifeCycle == LifeCycle.Prefab)
                    {
                        component.gameObject.SetActive(false);
                    }
                }
                else
                {
                    debug.Log("Unable to resolve component of {0} for {1}", type, behaviour.name);
                }
            }

            return component;
        }

        /// <summary>
        /// Try to resolve array of components, this should be used in other attribute process methods
        /// </summary>
        /// <param name="mono">object is expected as unity mono behaviour</param>
        /// <returns>true if you want to stop other attribute process methods</returns>
        private Component[] GetComponentsFromGameObject(object mono, Type type, InjectAttribute injectAttribute)
        {
            //not supported for transient or singleton injections
//            if (injectAttribute.LifeCycle == LifeCycle.Transient ||
//                injectAttribute.LifeCycle == LifeCycle.Singleton)
//            {
//                return null;
//            }

            var behaviour = mono as MonoBehaviour;

            if (behaviour == null) return null;

            //output components
            Component[] components = null;

            //resolve by inject component to the gameobject
            var injectComponentArray = injectAttribute as IComponentArrayResolvable;

            if (injectComponentArray == null)
            {
                throw new InvalidOperationException(
                    "You must use apply injectAttribute with IComponentArrayResolvable to resolve array of components");
                //unable to get it from gameObject
            }

            components = injectComponentArray.GetComponents(behaviour, type.GetElementType());

            //unable to get it from gameObject
            if (components == null || components.Length == 0)
            {
                debug.Log("Unable to resolve components of {0} for {1}, found {2} elements",
                    type.GetElementType(), behaviour.name, components != null ? components.Length : 0);
            }
            else
            {
                foreach (var component in components)
                {
                    ProcessInjectAttribute(component);
                }
            }

            return components;
        }


        private void Initialize(Type target = null, bool automaticBinding = false)
        {
            if (!Initialized)
            {
                _defaultInstance = this;
            }

            this.automaticBinding = automaticBinding;
            this.TargetType = target;
            container = new Container(this);

            initialized = true;

            InitialProcess();
        }

        #endregion

        #region Public members

        /// <summary>
        /// Get current scene name
        /// </summary>
        public string CurrentSceneName
        {
            get { return SceneManager.GetActiveScene().name; }
        }

        /// <summary>
        /// Shortcut to get current assembly or just return the default one of unity
        /// </summary>
        public Assembly CurrentAssembly
        {
            get
            {
                return TargetType == null
                    ? string.IsNullOrEmpty(assemblyName) ? Assembly.Load(DefaultAssemblyName) :
                    Assembly.Load(assemblyName)
                    : TargetType.Assembly;
            }
        }

        public void ResolveAction<T>(Action<T> action, LifeCycle lifeCycle = LifeCycle.Default,
            object resultFrom = null)
        {
            var arg = (T) Resolve(typeof(T), lifeCycle, resultFrom);
            action(arg);
        }

        public void ResolveAction<T1, T2>(Action<T1, T2> action,
            LifeCycle lifeCycle1 = LifeCycle.Default,
            LifeCycle lifeCycle2 = LifeCycle.Default,
            object resultFrom1 = null, object resultFrom2 = null)
        {
            var arg1 = (T1) Resolve(typeof(T1), lifeCycle1, resultFrom1);
            var arg2 = (T2) Resolve(typeof(T1), lifeCycle2, resultFrom2);
            action(arg1, arg2);
        }

        public Result ResolveFunc<Input, Result>(Func<Input, Result> func, LifeCycle lifeCycle = LifeCycle.Default,
            object resultFrom = null)
        {
            var arg = (Input) Resolve(typeof(Input), lifeCycle, resultFrom);
            return func(arg);
        }

        public Result ResolveFunc<Input1, Input2, Result>(Func<Input1, Input2, Result> func,
            LifeCycle lifeCycle1 = LifeCycle.Default,
            LifeCycle lifeCycle2 = LifeCycle.Default,
            object resultFrom1 = null,
            object resultFrom2 = null)
        {
            var arg1 = (Input1) Resolve(typeof(Input1), lifeCycle1, resultFrom1);
            var arg2 = (Input2) Resolve(typeof(Input2), lifeCycle2, resultFrom2);
            return func(arg1, arg2);
        }


        /// <summary>
        /// Process inject attributes in every mono behaviour in scene
        /// </summary>
        public void ProcessInjectAttributeForMonoBehaviours(bool ignoreComponents = false)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var allBehaviours = Object.FindObjectsOfType<MonoBehaviour>();

            var ignoredUnityEngineScripts = allBehaviours.Where(m =>
            {
                var ns = m.GetType().Namespace;
                return ns == null || !ns.StartsWith("UnityEngine");
            }).ToArray();

            var sortableBehaviours = Array.FindAll(ignoredUnityEngineScripts,
                b => b.GetType().GetCustomAttributes(typeof(ProcessingOrderAttribute), true).Any());

            var nonSortableBehaviours = ignoredUnityEngineScripts.Except(sortableBehaviours);

            if (sortableBehaviours.Any())
            {
                debug.Log("Found sortableBehaviours behavior: " + sortableBehaviours.Count());

                Array.Sort(sortableBehaviours);

                foreach (var mono in sortableBehaviours)
                {
                    if (mono)
                    {
                        ProcessInjectAttribute(mono);
                    }
                }
            }

            if (nonSortableBehaviours.Any())
            {
                debug.Log("Found nonSortableBehaviours behavior: " + nonSortableBehaviours.Count());

                if (debug.enableLogging)
                {
                    nonSortableBehaviours.Select(m => m.GetType().Name).Do(m => debug.Log(m));
                }

                foreach (var mono in nonSortableBehaviours)
                {
                    if (mono)
                    {
                        ProcessInjectAttribute(mono);
                    }
                }
            }
        }

        /// <summary>
        /// Process an unity Object for resolving every inject attributes inside
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parent"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T CreateInstance<T>(T obj, Transform parent = null) where T : Object
        {
            T clone = null;
            Component clonedComp = null;
            GameObject clonedGameObj = null;

            if (obj is GameObject)
            {
                clone = Object.Instantiate(obj, parent);
                clonedGameObj = clone as GameObject;
            }
            else
            {
                var comp = obj as Component;
                if (comp)
                {
                    clone = Object.Instantiate(comp.gameObject, parent).GetComponent(typeof(T)) as T;
                }
                else
                {
                    clone = Object.Instantiate(obj, parent);
                }

                clonedComp = clone as Component;
                if (clonedComp)
                {
                    foreach (var mono in clonedComp.gameObject.GetComponents(typeof(MonoBehaviour)))
                    {
                        ProcessInjectAttribute(mono);
                    }

                    //activate component before returning it
                    clonedComp.gameObject.SetActive(true);
                    return clonedComp as T;
                }
            }

            ProcessInjectAttribute(clone);

            //activate gameObject before returning it
            if (clonedGameObj)
            {
                clonedGameObj.SetActive(true);
            }

            return clone;
        }


        /// <summary>
        /// Read binding data to create registerObjects
        /// </summary>
        /// <param name="bindingSetting"></param>
        public void LoadBindingSetting(BindingSetting bindingSetting)
        {
            debug.Log("From BindingSetting, {0} settings found: ", bindingSetting.defaultSettings.Length);
            //binding for default setting 
            if (bindingSetting.defaultSettings.Length > 0)
            {
                foreach (var setting in bindingSetting.defaultSettings)
                {
                    BindFromSetting(setting);
                }

                if (bindingSetting.assemblyHolder != null)
                {
                    assemblyName = bindingSetting.assemblyHolder.name;
                }

                if (bindingSetting.autoProcessSceneObjects)
                {
                    ProcessInjectAttributeForMonoBehaviours(bindingSetting.ignoreGameComponent);
                }
            }
        }


        /// <summary>
        /// Load binding setting to inject for a type T in a scene
        /// </summary>
        /// <param name="bindingSetting">custom setting</param>
        /// <typeparam name="T">Type to inject</typeparam>
        public void LoadBindingSettingForType(Type type, BindingSetting bindingSetting = null)
        {
            if (!bindingSetting)
            {
                debug.Log("Try to load binding setting for {0} from resources folders", type.Name);

                //try to load setting by name format: type_scene
                bindingSetting = MyResources.Load<BindingSetting>("{0}_{1}"
                    , type.Name
                    , CurrentSceneName);

                if (!bindingSetting)
                {
                    //try to load setting by name format: type
                    bindingSetting = MyResources.Load<BindingSetting>(type.Name);
                    if (bindingSetting)
                    {
                        debug.Log("Found default setting for type {0}", type.Name);
                    }
                }
                else
                {
                    debug.Log("Found default setting for type {0} in scene {1}", type.Name
                        , CurrentSceneName);
                }
            }

            if (!bindingSetting || bindingSetting.defaultSettings.Length == 0)
            {
                debug.Log("Not found default Binding setting for {0}!", type.Name);
                return;
            }

            foreach (var setting in bindingSetting.defaultSettings)
            {
                BindFromSetting(setting, type);
            }

            if (bindingSetting.assemblyHolder != null)
            {
                assemblyName = bindingSetting.assemblyHolder.name;
            }

            if (bindingSetting.autoProcessSceneObjects)
            {
                ProcessInjectAttributeForMonoBehaviours(bindingSetting.ignoreGameComponent);
            }
        }

        /// <summary>
        /// Load binding setting to inject for a type T in a scene
        /// </summary>
        /// <param name="bindingSetting">custom setting</param>
        /// <typeparam name="T">Type to inject</typeparam>
        public void LoadBindingSettingForType<T>(BindingSetting bindingSetting = null)
        {
            LoadBindingSettingForType(typeof(T), bindingSetting);
        }

        /// <summary>
        /// load binding setting for current scene
        /// </summary>
        /// <param name="bindingSetting">custom setting</param>
        public void LoadBindingSettingForScene(InjectIntoBindingSetting bindingSetting = null)
        {
            if (!bindingSetting)
            {
                debug.Log("Load binding setting for type from resources folders");

                //try to load setting by name format: type_scene
                bindingSetting = MyResources.Load<InjectIntoBindingSetting>(CurrentSceneName);

                if (bindingSetting)
                {
                    debug.Log("Found default setting for scene {0}", CurrentSceneName);
                }
            }

            if (!bindingSetting)
            {
                debug.Log("Binding setting should not be null!");
                return;
            }

            //binding for default setting 
            if (bindingSetting.defaultSettings != null)
            {
                debug.Log("Process binding from default setting");
                foreach (var setting in bindingSetting.defaultSettings)
                {
                    BindFromSetting(setting);
                }
            }

            if (bindingSetting.autoProcessSceneObjects)
            {
                ProcessInjectAttributeForMonoBehaviours(bindingSetting.ignoreGameComponent);
            }
        }

        public void LoadBindingSetting(string settingName)
        {
            LoadBindingSetting(UnityIoC.MyResources.Load<InjectIntoBindingSetting>(settingName));
        }

        public void LoadBindingSetting(InjectIntoBindingSetting bindingSetting)
        {
            debug.Log("From InjectIntoBindingSetting, {0} settings found: ", bindingSetting.defaultSettings.Count);
            //binding for default setting 
            if (bindingSetting.defaultSettings.Count > 0)
            {
                foreach (var setting in bindingSetting.defaultSettings)
                {
                    BindFromSetting(setting);
                }

                if (bindingSetting.assemblyHolder != null)
                {
                    assemblyName = bindingSetting.assemblyHolder.name;
                }

                if (bindingSetting.autoProcessSceneObjects)
                {
                    ProcessInjectAttributeForMonoBehaviours(bindingSetting.ignoreGameComponent);
                }
            }
        }

        private Container defaultContainer
        {
            get { return (Container) container; }
        }


        public Container DefaultContainer
        {
            get { return container; }
        }

        public void Bind(Type typeToResolve, object instance)
        {
            container.Bind(typeToResolve, instance);
        }

        public void Bind(Type typeToResolve, Type concreteType, LifeCycle lifeCycle = LifeCycle.Default)
        {
            container.Bind(typeToResolve, concreteType, lifeCycle);
        }

        public void Bind<TTypeToResolve>(object instance)
        {
            Bind(typeof(TTypeToResolve), instance);
        }

        public TTypeToResolve Resolve<TTypeToResolve>(
            LifeCycle lifeCycle = LifeCycle.Default,
            object resolveFrom = null,
            params object[] parameters)
        {
            return (TTypeToResolve) Resolve(typeof(TTypeToResolve), lifeCycle, resolveFrom, parameters);
        }

        public object Resolve(
            Type typeToResolve,
            LifeCycle lifeCycle = LifeCycle.Default,
            object resolveFrom = null,
            params object[] parameters)
        {
            debug.Log("Start resolve type of {0}", typeToResolve);
            debug.Log("resolveFrom: " + (resolveFrom ?? "None"));
            return container.ResolveObject(typeToResolve, lifeCycle, resolveFrom, parameters);
        }

        #endregion

        #region Static members

        private static Context _defaultInstance;

        public static bool Initialized
        {
            get { return _defaultInstance != null && _defaultInstance.initialized; }
        }

        public static Context DefaultInstance
        {
            get { return GetDefaultInstance(); }
            set { _defaultInstance = value; }
        }

        public static T Instantiate<T>(T origin) where T : Component
        {
            return DefaultInstance.CreateInstance(origin);
        }

        public static T Instantiate<T>(T origin, Object parent) where T : Component
        {
            return DefaultInstance.CreateInstance(origin, parent as Transform);
        }

        public static Context GetDefaultInstance(object context, bool automaticBind = true,
            bool recreate = false)
        {
            return GetDefaultInstance(context.GetType(), automaticBind, recreate);
        }

        public static Context GetDefaultInstance(Type type = null, bool automaticBind = true,
            bool recreate = false)
        {
            if (_defaultInstance == null || recreate)
            {
                _defaultInstance = new Context(type, automaticBind);
            }

            return _defaultInstance;
        }

        public static void DisposeDefaultInstance()
        {
            if (_defaultInstance != null)
            {
                _defaultInstance.Dispose();
                _defaultInstance = null;
            }
        }

        public static object ResolveObject(
            Type typeToResolve,
            LifeCycle lifeCycle = LifeCycle.Default,
            object resolveFrom = null,
            params object[] parameters)
        {
            return GetDefaultInstance(typeToResolve).Resolve(typeToResolve, lifeCycle, resolveFrom, parameters);
        }

        public static T ResolveObject<T>(
            LifeCycle lifeCycle = LifeCycle.Default,
            object resolveFrom = null,
            params object[] parameters)
        {
            return (T) GetDefaultInstance(typeof(T)).Resolve(typeof(T), lifeCycle, resolveFrom, parameters);
        }

        public static void SetPropertyValue(object inputObject, string propertyName, object propertyVal)
        {
            Type type = inputObject.GetType();
            PropertyInfo propertyInfo = type.GetProperty(propertyName);

            var targetType = IsNullableType(propertyInfo.PropertyType)
                ? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                : propertyInfo.PropertyType;

            propertyVal = Convert.ChangeType(propertyVal, targetType);
            propertyInfo.SetValue(inputObject, propertyVal, null);
        }

        public static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }

        public static void Bind<T1, T2>(LifeCycle lifeCycle = LifeCycle.Default)
        {
            GetDefaultInstance(typeof(T1)).container.Bind<T1, T2>(lifeCycle);
        }

        public static void Bind<T>(LifeCycle lifeCycle = LifeCycle.Default)
        {
            GetDefaultInstance(typeof(T)).container.Bind<T>(lifeCycle);
        }

        public static void Bind(object instance)
        {
            var typeToResolve = instance.GetType();
            GetDefaultInstance(typeToResolve).Bind(typeToResolve, instance);
        }

        public static void Bind(BindingSetting bindingSetting)
        {
            if (bindingSetting.assemblyHolder)
            {
                var customAssembly = Assembly.Load(bindingSetting.assemblyHolder.name);
                if (customAssembly != null)
                {
                    var type = customAssembly.GetTypes().FirstOrDefault();
                    GetDefaultInstance(type);
                }
            }

            GetDefaultInstance().LoadBindingSetting(bindingSetting);
        }

        public static void Bind(InjectIntoBindingSetting bindingSetting)
        {
            
            if (bindingSetting.assemblyHolder)
            {
                var customAssembly = Assembly.Load(bindingSetting.assemblyHolder.name);
                if (customAssembly != null)
                {
                    var type = customAssembly.GetTypes().FirstOrDefault();
                    GetDefaultInstance(type);
                }
            }
            
            GetDefaultInstance().LoadBindingSetting(bindingSetting);
        }

        #endregion

//        public static T AddComponent<T>(GameObject gameObj) where T : Component
//        {
//            return GetDefaultInstance(typeof(T)).Resolve<T>();
//        }
    }
}