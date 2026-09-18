// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DataLayer.ExtraAuthClasses;
using PermissionParts;

namespace ScopeAuthorize
{
    public static class AbacConditionEvaluator
    {
        public static bool IsSatisfied(string conditionsJson, DateTime utcNow, PaidForModules userModules, Type resourceType)
        {
            var conditions = Parse(conditionsJson);
            if (conditions == null)
                return true;

            if (!TimeWindowAllows(conditions, utcNow))
                return false;

            if (conditions.RequiredModules.HasValue
                && conditions.RequiredModules.Value != PaidForModules.None
                && !userModules.HasFlag(conditions.RequiredModules.Value))
                return false;

            if (conditions.ResourceTypeNames != null && conditions.ResourceTypeNames.Length > 0)
            {
                var typeName = resourceType?.Name;
                if (string.IsNullOrEmpty(typeName) ||
                    !conditions.ResourceTypeNames.Contains(typeName, StringComparer.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        public static AssignmentConditions Parse(string conditionsJson)
        {
            if (string.IsNullOrWhiteSpace(conditionsJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<AssignmentConditions>(conditionsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool TimeWindowAllows(AssignmentConditions conditions, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(conditions.FromTimeUtc) && string.IsNullOrWhiteSpace(conditions.ToTimeUtc))
                return true;

            var current = utcNow.TimeOfDay;
            TimeSpan? from = TryParseTime(conditions.FromTimeUtc);
            TimeSpan? to = TryParseTime(conditions.ToTimeUtc);

            if (from.HasValue && to.HasValue)
            {
                if (from.Value <= to.Value)
                    return current >= from.Value && current <= to.Value;
                // Overnight window, e.g. 22:00-06:00
                return current >= from.Value || current <= to.Value;
            }

            if (from.HasValue)
                return current >= from.Value;
            if (to.HasValue)
                return current <= to.Value;
            return true;
        }

        private static TimeSpan? TryParseTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return TimeSpan.TryParse(value, out var parsed) ? parsed : (TimeSpan?)null;
        }
    }
}
