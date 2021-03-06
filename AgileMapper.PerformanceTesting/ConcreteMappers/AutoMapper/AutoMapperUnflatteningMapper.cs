﻿namespace AgileObjects.AgileMapper.PerformanceTesting.ConcreteMappers.AutoMapper
{
    using AbstractMappers;
    using global::AutoMapper;
    using static TestClasses.Flattening;

    public class AutoMapperUnflatteningMapper : UnflatteningMapperBase
    {
        private IMapper _mapper;

        public override void Initialise()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ModelObject, ModelDto>().ReverseMap();
            });

            _mapper = config.CreateMapper();
        }

        protected override ModelObject Unflatten(ModelDto dto)
            => _mapper.Map<ModelDto, ModelObject>(dto);
    }
}