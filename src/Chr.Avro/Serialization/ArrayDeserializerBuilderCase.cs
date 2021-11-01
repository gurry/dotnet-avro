namespace Chr.Avro.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Chr.Avro.Abstract;
    using Chr.Avro.Infrastructure;

    /// <summary>
    /// Provides a base implementation for deserializer builder cases that match <see cref="ArraySchema" />.
    /// </summary>
    public abstract class ArrayDeserializerBuilderCase : DeserializerBuilderCase
    {
        /// <remarks>
        /// This override includes additional conditions to handle conversions to arrays and other
        /// collection types. If none match, the base implementation is used.
        /// </remarks>
        /// <inheritdoc />
        protected override Expression BuildConversion(Expression value, Type target)
        {
            if (!value.Type.IsArray && (target.IsArray || target.IsAssignableFrom(typeof(ArraySegment<>).MakeGenericType(target.GenericTypeArguments))))
            {
                var toArray = value.Type
                    .GetMethod("ToArray", Type.EmptyTypes);

                value = Expression.Call(value, toArray);
            }
            else if (target.Assembly == typeof(ImmutableInterlocked).Assembly)
            {
                if (target.IsAssignableFrom(typeof(ImmutableQueue<>).MakeGenericType(target.GenericTypeArguments)))
                {
                    var createRange = typeof(ImmutableQueue)
                        .GetMethod(nameof(ImmutableQueue.CreateRange))
                        .MakeGenericMethod(target.GenericTypeArguments);

                    value = Expression.Call(null, createRange, value);
                }
                else if (target.IsAssignableFrom(typeof(ImmutableStack<>).MakeGenericType(target.GenericTypeArguments)))
                {
                    var createRange = typeof(ImmutableStack)
                        .GetMethod(nameof(ImmutableStack.CreateRange))
                        .MakeGenericMethod(target.GenericTypeArguments);

                    value = Expression.Call(null, createRange, value);
                }
                else
                {
                    var toImmutable = value.Type
                        .GetMethod("ToImmutable", Type.EmptyTypes);

                    value = Expression.Call(value, toImmutable);
                }
            }

            return base.BuildConversion(value, target);
        }

        /// <summary>
        /// Builds an <see cref="Expression" /> that represents instantiating a new collection.
        /// </summary>
        /// <remarks>
        /// This method includes conditions to support deserializing to concrete collection types
        /// that ship with .NET.
        /// </remarks>
        /// <param name="type">
        /// An enumerable <see cref="Type" />.
        /// </param>
        /// <returns>
        /// An <see cref="Expression" /> representing the creation of a collection that can be
        /// converted to <paramref name="type" />.
        /// </returns>
        protected virtual Expression BuildIntermediateCollection(Type type)
        {
            var itemType = type.GetEnumerableType() ?? throw new ArgumentException($"{type} is not an enumerable type.");

            if (type.IsArray || type.IsAssignableFrom(typeof(ArraySegment<>).MakeGenericType(itemType)) || type.IsAssignableFrom(typeof(ImmutableArray<>).MakeGenericType(itemType)))
            {
                var createBuilder = typeof(ImmutableArray)
                    .GetMethod(nameof(ImmutableArray.CreateBuilder), Type.EmptyTypes)
                    .MakeGenericMethod(itemType);

                return Expression.Call(null, createBuilder);
            }

            if (type.IsAssignableFrom(typeof(ImmutableHashSet<>).MakeGenericType(itemType)))
            {
                var createBuilder = typeof(ImmutableHashSet)
                    .GetMethod(nameof(ImmutableHashSet.CreateBuilder), Type.EmptyTypes)
                    .MakeGenericMethod(itemType);

                return Expression.Call(null, createBuilder);
            }

            if (type.IsAssignableFrom(typeof(ImmutableList<>).MakeGenericType(itemType)))
            {
                var createBuilder = typeof(ImmutableList)
                    .GetMethod(nameof(ImmutableList.CreateBuilder), Type.EmptyTypes)
                    .MakeGenericMethod(itemType);

                return Expression.Call(null, createBuilder);
            }

            if (type.IsAssignableFrom(typeof(ImmutableSortedSet<>).MakeGenericType(itemType)))
            {
                var createBuilder = typeof(ImmutableSortedSet)
                    .GetMethod(nameof(ImmutableSortedSet.CreateBuilder), Type.EmptyTypes)
                    .MakeGenericMethod(itemType);

                return Expression.Call(null, createBuilder);
            }

            if (type.IsAssignableFrom(typeof(HashSet<>).MakeGenericType(itemType)))
            {
                return Expression.New(typeof(HashSet<>).MakeGenericType(itemType).GetConstructor(Type.EmptyTypes));
            }

            if (type.IsAssignableFrom(typeof(SortedSet<>).MakeGenericType(itemType)))
            {
                return Expression.New(typeof(SortedSet<>).MakeGenericType(itemType).GetConstructor(Type.EmptyTypes));
            }

            if (type.IsAssignableFrom(typeof(Collection<>).MakeGenericType(itemType)))
            {
                return Expression.New(typeof(Collection<>).MakeGenericType(itemType).GetConstructor(Type.EmptyTypes));
            }

            return Expression.New(typeof(List<>).MakeGenericType(itemType).GetConstructor(Type.EmptyTypes));
        }

        /// <summary>
        /// Gets a constructor that can be used to instantiate a collection type.
        /// </summary>
        /// <param name="type">
        /// A collection <see cref="Type" />.
        /// </param>
        /// <returns>
        /// A <see cref="ConstructorInfo" /> from <paramref name="type" /> if one matches;
        /// <c>null</c> otherwise.
        /// </returns>
        protected virtual ConstructorInfo? GetCollectionConstructor(Type type)
        {
            var itemType = type.GetEnumerableType() ?? throw new ArgumentException($"{type} is not an enumerable type.");

            return type.GetConstructors()
                .Where(constructor => constructor.GetParameters().Count() == 1)
                .FirstOrDefault(constructor => constructor.GetParameters().First().ParameterType
                    .IsAssignableFrom(typeof(IEnumerable<>).MakeGenericType(itemType)));
        }

        /// <summary>
        /// Gets the item <see cref="Type" /> of an enumerable <see cref="Type" />.
        /// </summary>
        /// <param name="type">
        /// A <see cref="Type" /> object that describes a generic enumerable.
        /// </param>
        /// <returns>
        /// If <paramref name="type" /> implements (or is) <see cref="IEnumerable{T}" />, its type
        /// argument; <c>null</c> otherwise.
        /// </returns>
        protected virtual Type? GetEnumerableType(Type type)
        {
            return type.GetEnumerableType();
        }
    }
}