using BusinessObjects.Products;
using BusinessObjects.Shipping;
using DTOs.Common;
using DTOs.Shipping;
using Repositories.Helpers;

namespace Services.Extensions
{
    public static class ShippingMethodMappingExtensions
    {
        /// <summary>
        /// Maps PagedList<ShippingMethod> to PagedResponse<ShippingMethodDTO>
        /// </summary>
        public static PagedResponse<ShippingMethodDTO> ToPagedResponse(this PagedList<ShippingMethod> pagedList)
        {
            var mappedItems = pagedList.Select(ToDTO).ToList();

            return new PagedResponse<ShippingMethodDTO>
            {
                Data = mappedItems,
                CurrentPage = pagedList.MetaData.CurrentPage,
                PageSize = pagedList.MetaData.PageSize,
                TotalPages = pagedList.MetaData.TotalPages,
                TotalCount = pagedList.MetaData.TotalCount,
                HasNextPage = pagedList.MetaData.CurrentPage < pagedList.MetaData.TotalPages,
                HasPreviousPage = pagedList.MetaData.CurrentPage > 1,
                IsSuccess = true
            };
        }

        /// <summary>
        /// Maps single ShippingMethod to ShippingMethodDTO
        /// </summary>
        public static ShippingMethodDTO ToDTO(this ShippingMethod shippingMethod)
        {
            return new ShippingMethodDTO
            {
                Id = shippingMethod.Id,
                Name = shippingMethod.Name.ToString(),
                Description = shippingMethod.Description,
                Fee = shippingMethod.Fee,
                FreeShippingThreshold = shippingMethod.FreeShippingThreshold,
                EstimatedDays = shippingMethod.EstimatedDays,
                MinDeliveryDays = shippingMethod.MinDeliveryDays,
                MaxDeliveryDays = shippingMethod.MaxDeliveryDays,
                IsActive = shippingMethod.IsActive,
                SortOrder = shippingMethod.SortOrder,
                CreatedAt = shippingMethod.CreatedAt,
                UpdatedAt = shippingMethod.UpdatedAt,
                CreatedByName = null,
                UpdatedByName = null
            };
        }

        /// <summary>
        /// Maps collection of ShippingMethod to collection of ShippingMethodDTO
        /// </summary>
        public static IEnumerable<ShippingMethodDTO> ToDTOs(this IEnumerable<ShippingMethod> shippingMethods)
        {
            return shippingMethods.Select(ToDTO);
        }

        /// <summary>
        /// Maps CreateShippingMethodRequest to ShippingMethod entity
        /// </summary>
        public static ShippingMethod ToEntity(this CreateShippingMethodRequest request)
        {
            if (!Enum.TryParse<ShippingCategory>(request.Name, ignoreCase: true, out var category))
            {
                throw new ArgumentException($"Invalid shipping category: {request.Name}");
            }

            return new ShippingMethod
            {
                Id = Guid.NewGuid(),
                Name = category,
                Description = request.Description,
                Fee = request.Fee,
                FreeShippingThreshold = request.FreeShippingThreshold,
                EstimatedDays = request.EstimatedDays,
                MinDeliveryDays = request.MinDeliveryDays,
                MaxDeliveryDays = request.MaxDeliveryDays,
                IsActive = request.IsActive,
                SortOrder = request.SortOrder
            };
        }

        /// <summary>
        /// Updates existing ShippingMethod entity with data from UpdateShippingMethodRequest
        /// </summary>
        public static void UpdateFromRequest(this ShippingMethod entity, UpdateShippingMethodRequest request)
        {
            if (Enum.TryParse<ShippingCategory>(request.Name.ToString(), true, out var category))
            {
                entity.Name = category;
            }
            if (request.Description != null)
                entity.Description = request.Description;

            if (request.Fee.HasValue)
                entity.Fee = request.Fee.Value;

            if (request.FreeShippingThreshold.HasValue)
                entity.FreeShippingThreshold = request.FreeShippingThreshold;

            if (request.EstimatedDays.HasValue)
                entity.EstimatedDays = request.EstimatedDays.Value;

            if (request.MinDeliveryDays.HasValue)
                entity.MinDeliveryDays = request.MinDeliveryDays;

            if (request.MaxDeliveryDays.HasValue)
                entity.MaxDeliveryDays = request.MaxDeliveryDays;

            if (request.IsActive.HasValue)
                entity.IsActive = request.IsActive.Value;

            if (request.SortOrder.HasValue)
                entity.SortOrder = request.SortOrder.Value;
        }

        /// <summary>
        /// Validates delivery days range for ShippingMethod
        /// </summary>
        public static bool IsDeliveryDaysValid(this ShippingMethod shippingMethod)
        {
            if (!shippingMethod.MinDeliveryDays.HasValue || !shippingMethod.MaxDeliveryDays.HasValue)
                return true;

            return shippingMethod.MinDeliveryDays.Value <= shippingMethod.MaxDeliveryDays.Value;
        }

        /// <summary>
        /// Creates a successful PagedResponse with message
        /// </summary>
        public static PagedResponse<T> WithSuccessMessage<T>(this PagedResponse<T> response, string message)
        {
            response.Message = message;
            response.IsSuccess = true;
            return response;
        }

        /// <summary>
        /// Creates a failed PagedResponse with error message
        /// </summary>
        public static PagedResponse<T> WithErrorMessage<T>(this PagedResponse<T> response, string message, List<string>? errors = null)
        {
            response.Message = message;
            response.IsSuccess = false;
            response.Errors = errors ?? new List<string>();
            return response;
        }
    }
}