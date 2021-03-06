﻿//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//
//namespace Unity.Linq
//{
//    public static partial class GameObjectExtensions
//    {
//        public partial struct DescendantsEnumerable
//        {
//            public OfComponentEnumerable OfComponent(Type type)
//            {
//                return new OfComponentEnumerable(ref this, type);
//            }
//        }
//
//        public struct OfComponentEnumerable : IEnumerable
//        {
//            DescendantsEnumerable parent;
//            Type type;
//
//            public OfComponentEnumerable(ref DescendantsEnumerable parent, Type type)
//            {
//                this.parent = parent;
//                this.type = type;
//            }
//
//            public OfComponentEnumerator GetEnumerator()
//            {
//                return new OfComponentEnumerator(ref parent);
//            }
//
//
//            IEnumerator IEnumerable.GetEnumerator()
//            {
//                return GetEnumerator();
//            }
//
//            #region LINQ
//
//            public object First()
//            {
//                var e = this.GetEnumerator();
//                try
//                {
//                    if (e.MoveNext())
//                    {
//                        return e.Current;
//                    }
//                    else
//                    {
//                        throw new InvalidOperationException("sequence is empty.");
//                    }
//                }
//                finally
//                {
//                    e.Dispose();
//                }
//            }
//
//            public object FirstOrDefault()
//            {
//                var e = this.GetEnumerator();
//                try
//                {
//                    return (e.MoveNext())
//                        ? e.Current
//                        : null;
//                }
//                finally
//                {
//                    e.Dispose();
//                }
//            }
//
//            /// <summary>Use internal iterator for performance optimization.</summary>
//            public void ForEach(Action<object> action)
//            {
//                if (parent.withSelf)
//                {
//                    object component = null;
//#if UNITY_EDITOR
//                    parent.origin.GetComponents<T>(componentCache);
//                    if (componentCache.Count != 0)
//                    {
//                        component = componentCache[0];
//                        componentCache.Clear();
//                    }
//#else
//                           component = parent.origin.GetComponent<T>();
//#endif
//
//                    if (component != null)
//                    {
//                        action(component);
//                    }
//                }
//
//                var originTransform = parent.origin.transform;
//                OfComponentDescendantsCore(ref originTransform, ref action);
//            }
//
//
//            public object[] ToArray()
//            {
//                var array = new object[4];
//                var len = ToArrayNonAlloc(ref array);
//                if (array.Length != len)
//                {
//                    Array.Resize(ref array, len);
//                }
//
//                return array;
//            }
//
//#if UNITY_EDITOR
//            static List<object> componentCache = new List<object>(); // for no allocate on UNITY_EDITOR
//#endif
//
//            void OfComponentDescendantsCore(ref Transform transform, ref Action<T> action)
//            {
//                if (!parent.descendIntoChildren(transform)) return;
//
//                var childCount = transform.childCount;
//                for (int i = 0; i < childCount; i++)
//                {
//                    var child = transform.GetChild(i);
//
//                    T component = default(T);
//#if UNITY_EDITOR
//                    child.GetComponents<T>(componentCache);
//                    if (componentCache.Count != 0)
//                    {
//                        component = componentCache[0];
//                        componentCache.Clear();
//                    }
//#else
//                           component = child.GetComponent<T>();
//#endif
//
//                    if (component != null)
//                    {
//                        action(component);
//                    }
//
//                    OfComponentDescendantsCore(ref child, ref action);
//                }
//            }
//
//            void OfComponentDescendantsCore(ref Transform transform, ref int index, ref T[] array)
//            {
//                if (!parent.descendIntoChildren(transform)) return;
//
//                var childCount = transform.childCount;
//                for (int i = 0; i < childCount; i++)
//                {
//                    var child = transform.GetChild(i);
//                    T component = default(T);
//#if UNITY_EDITOR
//                    child.GetComponents<T>(componentCache);
//                    if (componentCache.Count != 0)
//                    {
//                        component = componentCache[0];
//                        componentCache.Clear();
//                    }
//#else
//                           component = child.GetComponent<T>();
//#endif
//
//                    if (component != null)
//                    {
//                        if (array.Length == index)
//                        {
//                            var newSize = (index == 0) ? 4 : index * 2;
//                            Array.Resize(ref array, newSize);
//                        }
//
//                        array[index++] = component;
//                    }
//
//                    OfComponentDescendantsCore(ref child, ref index, ref array);
//                }
//            }
//
//            /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
//            public int ToArrayNonAlloc(ref object[] array)
//            {
//                var index = 0;
//                if (parent.withSelf)
//                {
//                    object component = null;
//
//#if UNITY_EDITOR
//                    parent.origin.GetComponents<T>(componentCache);
//                    if (componentCache.Count != 0)
//                    {
//                        component = componentCache[0];
//                        componentCache.Clear();
//                    }
//#else
//                           component = parent.origin.GetComponent<T>();
//#endif
//
//                    if (component != null)
//                    {
//                        if (array.Length == index)
//                        {
//                            var newSize = (index == 0) ? 4 : index * 2;
//                            Array.Resize(ref array, newSize);
//                        }
//
//                        array[index++] = component;
//                    }
//                }
//
//                var originTransform = parent.origin.transform;
//                OfComponentDescendantsCore(ref originTransform, ref index, ref array);
//
//                return index;
//            }
//
//            #endregion
//        }
//        
//        
//        public struct OfComponentEnumerator
//        {
//            Enumerator enumerator; // enumerator is mutable
//            T current;
//
//#if UNITY_EDITOR
//            static List<T> componentCache = new List<T>(); // for no allocate on UNITY_EDITOR
//#endif
//
//            public OfComponentEnumerator(ref AncestorsEnumerable parent)
//            {
//                this.enumerator = parent.GetEnumerator();
//                this.current = default(T);
//            }
//
//            public bool MoveNext()
//            {
//                while (enumerator.MoveNext())
//                {
//#if UNITY_EDITOR
//                    enumerator.Current.GetComponents<T>(componentCache);
//                    if (componentCache.Count != 0)
//                    {
//                        current = componentCache[0];
//                        componentCache.Clear();
//                        return true;
//                    }
//#else
//                        
//                        var component = enumerator.Current.GetComponent<T>();
//                        if (component != null)
//                        {
//                            current = component;
//                            return true;
//                        }
//#endif
//                }
//
//                return false;
//            }
//
//            public T Current { get { return current; } }
//            object IEnumerator.Current { get { return current; } }
//            public void Dispose() { }
//            public void Reset() { throw new NotSupportedException(); }
//        }
//    }
//    }
//}