﻿using System.Threading.Tasks;
using TinyFp;
using TinyFpTest.Models;
using TinyFpTest.Services.Api;

namespace TinyFpTest.Services.Details
{
    public class DetailsService : IDetailsService
    {
        public Task<Either<ApiError, ProductDetails>> GetDetailsAsync(string productName)
            => Task.FromResult(Either<ApiError, ProductDetails>.Right(new ProductDetails()));
    }
}
