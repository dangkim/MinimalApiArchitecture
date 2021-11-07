﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalApiArchitecture.Api.Data;
using MinimalApiArchitecture.Api.Entities;

namespace MinimalApiArchitecture.Api.Features.Products.Queries;

public class GetProducts
{
    public class Query : IRequest<List<Response>>
    {

    }

    public class MappingProfile : Profile
    {
        public MappingProfile() => CreateMap<Product, Response>();
    }

    public class Handler : IRequestHandler<Query, List<Response>>
    {
        private readonly ApiDbContext _context;
        private readonly AutoMapper.IConfigurationProvider _configuration;

        public Handler(ApiDbContext context, AutoMapper.IConfigurationProvider configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public Task<List<Response>> Handle(Query request, CancellationToken cancellationToken) =>
            _context.Products
                .ProjectTo<Response>(_configuration)
                .ToListAsync();
    }

    public record Response(int ProductId, string Name, string Description, double Price);
}