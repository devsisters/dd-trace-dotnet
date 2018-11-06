using System;

namespace Datadog.Trace.ClrProfiler
{
    /// <summary>
    /// Provides helper methods to access object properties by emitting IL.
    /// </summary>
    public static class PropertyAccessor
    {
        /// <summary>
        /// Gets the value of the property in <paramref name="source"/>
        /// specified by <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="source">The value that contains the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The value of the property.</returns>
        public static object GetProperty(this object source, string propertyName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var accessor = DynamicMethodBuilder<Func<object, object>>.GetOrCreateMethodCallDelegate(
                source.GetType(),
                "get_" + propertyName);

            return accessor(source);
        }
    }
}
