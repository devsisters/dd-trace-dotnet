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
        /// <param name="value">The value of the property, or <c>null</c> if the property is not found.</param>
        /// <returns><c>true</c> if the property exists and <paramref name="value"/> was set; otherwise, <c>false</c>.</returns>
        public static bool TryGetPropertyValue(this object source, string propertyName, out object value)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }

            if (propertyName == null) { throw new ArgumentNullException(nameof(propertyName)); }

            var accessor = DynamicMethodBuilder<Func<object, object>>.GetOrCreateMethodCallDelegate(
                source.GetType(),
                "get_" + propertyName);

            if (accessor == null)
            {
                value = null;
                return false;
            }

            value = accessor(source);
            return true;
        }
    }
}
