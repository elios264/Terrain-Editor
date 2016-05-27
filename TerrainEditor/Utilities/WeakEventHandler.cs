using System;
using System.Linq;
using System.Linq.Expressions;

namespace TerrainEditor.Utilities
{
    /// <summary>
    /// replaces the original delegate with a delagate that calls the original delagate stored in a WeakReference
    /// so the original delegate won't be strong referenced by the event, but instead it'll strong reference this class, 
    /// which still results in a memory leak but a smaller one 
    /// </summary>
    /// <typeparam name="T">The type of the delegate that is going to weak reference</typeparam>
    public class WeakEventHandler<T>
    {
        private readonly WeakReference m_target;
        private readonly T m_weakDelegate;

        static WeakEventHandler()
        {
            if (!typeof(T).IsSubclassOf(typeof(Delegate)))
                throw new ArgumentException($"{typeof(T).Name} must be a delegate type");
        }

        public WeakEventHandler(T handler)
        {
            var @delegate = handler as Delegate;

            if (@delegate.Target == null)
                throw new ArgumentException("WeakEventHandler only makes sense with instance methods");

            m_target = new WeakReference(@delegate.Target);

            var parameters = @delegate.Method
                .GetParameters()
                .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
                .ToArray();


            var targetDeclaration = Expression.Variable(typeof(object)); //object target;
            var targetAssignement = Expression.Assign(targetDeclaration, Expression.Invoke((Expression<Func<object>>) ( () => m_target.Target ), Enumerable.Empty<Expression>())); // target = m_target.Target;
            var ifCondition = Expression.NotEqual(targetDeclaration, Expression.Constant(null, typeof(object))); // target != null
            var cast = Expression.Convert(targetDeclaration,m_target.Target.GetType()); // (T) target
            var callDelegate = Expression.Call(cast, @delegate.Method, parameters.Cast<Expression>()); //m_method.Invoke(target,params);
            var ifthenInvoke = Expression.IfThen(ifCondition, callDelegate);

            //void Invoke(params)
            //{
            //    object target = m_target.Target;
            //    if (target != null)
            //    {
            //        ((T)target).Invoke(params)
            //    }
            //}}
            m_weakDelegate = Expression.Lambda<T>(Expression.Block(targetDeclaration.ToNewArray(), targetAssignement, ifthenInvoke),parameters).Compile();
        }

        public static implicit operator T(WeakEventHandler<T> handler)
        {
            return handler.m_weakDelegate;
        }

    }
}
