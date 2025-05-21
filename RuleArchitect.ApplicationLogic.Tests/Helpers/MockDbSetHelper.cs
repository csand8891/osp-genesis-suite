// File: [YourTestProjectName]/Helpers/MockDbSetHelper.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query; // Required for IAsyncQueryProvider
using Microsoft.EntityFrameworkCore.Query.Internal;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions; // Required for Expression
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RuleArchitect.ApplicationLogic.Tests.Helpers
{
    // TestAsyncEnumerator remains the same as you have it
    public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public T Current => _inner.Current;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
        public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);
        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
    }

    // Add TestAsyncEnumerable
    public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }
        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    // Add TestAsyncQueryProvider
    public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        // Inside TestAsyncQueryProvider<TEntity> class in MockDbSetHelper.cs

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            // Get the single generic argument type from TResult (e.g., if TResult is Task<List<YourEntity>>, this gets List<YourEntity>)
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];

            // Find the generic method IQueryProvider.Execute<TResult>(Expression expression)
            // This approach is more robust across different .NET versions than relying on specific GetMethod overloads with named parameters.
            MethodInfo executeMethodInfo = typeof(IQueryProvider).GetMethods()
                .Single(m => m.Name == nameof(IQueryProvider.Execute) // Match method name
                             && m.IsGenericMethodDefinition          // Ensure it's a generic method definition (like Execute<TResult>)
                             && m.GetGenericArguments().Length == 1  // Ensure it has one generic type parameter
                             && m.GetParameters().Length == 1        // Ensure it takes one method parameter
                             && m.GetParameters()[0].ParameterType == typeof(Expression)); // Ensure that parameter is of type Expression

            // Make the generic method concrete with the actual result type
            var concreteExecuteMethod = executeMethodInfo.MakeGenericMethod(expectedResultType);

            // Invoke the method on the _inner provider, not 'this'
            var executionResult = concreteExecuteMethod.Invoke(_inner, new object[] { expression });

            // Wrap the synchronous result in a Task, as expected by ExecuteAsync
            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                                         .MakeGenericMethod(expectedResultType)
                                         .Invoke(null, new[] { executionResult });
        }
    }

    public static class MockDbSetHelper
    {
        public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            // Use the TestAsyncQueryProvider
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider)
                   .Returns(new TestAsyncQueryProvider<T>(queryable.Provider)); // MODIFIED

            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);

            // Enumerate the sourceList directly (the copy was not the primary issue but this is cleaner)
            // Ensure enumeration happens on a copy of the sourceList
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator())
                   .Returns(() => new List<T>(sourceList).GetEnumerator()); // Create copy here

            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(new List<T>(sourceList).GetEnumerator())); // Create copy here

            // Callbacks for Add/AddRange modify the original sourceList. This is correct.
            mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(item => sourceList.Add(item));
            mockSet.Setup(m => m.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(items => sourceList.AddRange(items));

            return mockSet;
        }
    }
}