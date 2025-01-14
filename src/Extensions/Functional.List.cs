﻿using System.Diagnostics.Contracts;

namespace TinyFp.Extensions
{
    public static partial class Functional
    {
        [Pure]
        public static IEnumerable<T> Filter<T>(this IEnumerable<T> @this, Func<T, bool> predicate) 
            => @this.Where(predicate);

        [Pure]
        public static Unit ForEach<T>(this IEnumerable<T> @this, Action<T> action)
        {
            foreach (var item in @this)
            {
                action(item);
            }
            return Unit.Default;
        }

        [Pure]
        public static S Fold<S, T>(this IEnumerable<T> @this, S state, Func<S, T, S> folder)
        {
            @this.ForEach(_ => state = folder(state, _));
            return state;
        }

        [Pure]
        public static T Reduce<T>(this IEnumerable<T> @this, Func<T, T, T> reducer) 
            => @this.Fold(default, reducer);

        [Pure]
        public static IEnumerable<R> Map<T, R>(this IEnumerable<T> @this, Func<T, R> map)
            => @this.Select(map);
    }
}
