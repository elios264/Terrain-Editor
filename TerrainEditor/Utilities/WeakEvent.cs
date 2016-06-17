using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TerrainEditor.Utilities
{
    /// <summary>
    /// Created from a normal event every time you subscribe through this it'll store a weak reference to your delegate target
    /// everytime the event is invoked it'll either call your delegate or if the reference is lost it'll unsubscribe the proxy delegate
    /// from the event
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
    public class WeakEvent<TDelegate> where TDelegate : class 
    {
        private readonly object m_eventOwner;
        private readonly EventInfo m_eventInfo;
        private readonly ConditionalWeakTable<object,List<Tuple<WeakReference,TDelegate>>>  m_realWeakMapping;

        static WeakEvent()
        {
            if (!typeof(TDelegate).IsSubclassOf(typeof(Delegate)))
                throw new ArgumentException($"{typeof(TDelegate).Name} must be a delegate type");
        }
        public WeakEvent(object ownerOrType, string eventName)
        {
            EventInfo eventInfo;

            if (ownerOrType is Type)
            {
                eventInfo = (ownerOrType as Type).GetEvent(eventName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                ownerOrType = null;
            }
            else
                eventInfo = ownerOrType.GetType().GetEvent(eventName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            if (eventInfo.EventHandlerType != typeof(TDelegate))
                throw new ArgumentException($"{eventName} is of type {eventInfo.EventHandlerType.Name} which does not match {typeof(TDelegate).Name}");

            m_eventOwner = ownerOrType;
            m_eventInfo = eventInfo;
            m_realWeakMapping = new ConditionalWeakTable<object, List<Tuple<WeakReference, TDelegate>>>();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public void add(TDelegate handler)
        {
            var del = handler as Delegate;
            if (del.Target == null)
            {
                m_eventInfo.AddEventHandler(m_eventOwner, del);
            }
            else
            {
                var weakEventHandler = new WeakEventHandler(m_eventInfo, m_eventOwner, handler);

                m_eventInfo.AddEventHandler(
                    m_eventOwner,
                    weakEventHandler.WeakDelegate as Delegate);

                //this is only for the remove ability
                m_realWeakMapping
                    .GetOrCreateValue(del.Target)
                    .Add(Tuple.Create(new WeakReference(handler), weakEventHandler.WeakDelegate));
            }
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public void remove(TDelegate handler)
        {
            var del = handler as Delegate;
            if (del.Target == null)
            {
                m_eventInfo.RemoveEventHandler(m_eventOwner, del);
            }
            else
            {
                TDelegate weakDelegate;
                List<Tuple<WeakReference, TDelegate>> list;
                if (m_realWeakMapping.TryGetValue(del.Target, out list) && (weakDelegate = list.FirstOrDefault(t => del.Equals(t.Item1.Target))?.Item2) != null)
                {
                    m_eventInfo.RemoveEventHandler(m_eventOwner, weakDelegate as Delegate);
                }
            }
        }

        public static WeakEvent<TDelegate> operator +(WeakEvent<TDelegate> weakEvent, TDelegate right)
        {
            weakEvent.add(right);
            return weakEvent;
        }
        public static WeakEvent<TDelegate> operator -(WeakEvent<TDelegate> weakEvent, TDelegate right)
        {
            weakEvent.remove(right);
            return weakEvent;
        }

        private class WeakEventHandler
        {
            private readonly WeakReference m_target;
            public TDelegate WeakDelegate { get; }

            public WeakEventHandler(EventInfo info, object owner, TDelegate handler)
            {
                var @delegate = handler as Delegate;

                if (@delegate.Target == null)
                    throw new ArgumentException("WeakEvent subscription only makes sense with instance methods");

                m_target = new WeakReference(@delegate.Target);

                var parameters = @delegate.Method
                    .GetParameters()
                    .Select(parameter => Expression.Parameter(parameter.ParameterType))
                    .ToArray();

                var targetDeclaration = Expression.Variable(typeof(object)); //object target;
                var targetAssignement = Expression.Assign(targetDeclaration, Expression.Invoke((Expression<Func<object>>)(() => m_target.Target), Enumerable.Empty<Expression>())); // target = m_target.Target;
                var ifCondition = Expression.NotEqual(targetDeclaration, Expression.Constant(null, typeof(object))); // target != null
                var cast = Expression.Convert(targetDeclaration, m_target.Target.GetType()); // (T) target
                var callDelegate = Expression.Call(cast, @delegate.Method, parameters.Cast<Expression>()); //m_method.Invoke(target,params);
                var weakDelAcccess = Expression.Convert(Expression.Invoke((Expression<Func<Delegate>>)(() => WeakDelegate as Delegate), Enumerable.Empty<Expression>()), typeof(TDelegate));
                var removeHandler = owner == null // owner.event -= WeakDelegate 
                    ? Expression.Call(info.RemoveMethod, weakDelAcccess)
                    : Expression.Call(Expression.Constant(owner, info.DeclaringType), info.RemoveMethod, weakDelAcccess);
                var ifthenElseInvoke = Expression.IfThenElse(ifCondition, callDelegate, removeHandler); //if
                WeakDelegate = Expression.Lambda<TDelegate>(Expression.Block(targetDeclaration.IntoANewArray(), targetAssignement, ifthenElseInvoke), parameters).Compile();
                /*void Invoke(params)
                {
                    object target = m_target.Target;
                    if (target != null)
                        ((T)target).Invoke(params)
                    else
                        owner.event -= WeakDelegate
                }*/
            }
        }
    }
}