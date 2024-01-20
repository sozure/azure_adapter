using AutoMapper;
using VGManager.Adapter.Models;
using VGManager.Adapter.Models.Kafka;

namespace SMP.Soffie.NeptunAdapter.Services.MapperProfiles;

public class CommandMessageProfile : Profile
{
    public CommandMessageProfile()
    {
        CreateMap<CommandMessageBase, VGManagerAdapterCommandResponse>()
            .ForMember(responseMessage => responseMessage.CommandInstanceId, o => o.MapFrom(message => message.InstanceId))
            .ForMember(responseMessage => responseMessage.IsSuccess, o => o.MapFrom(message => true))
            .ForMember(responseMessage => responseMessage.Payload, o => o.Ignore())
            .ForMember(responseMessage => responseMessage.Origin, o => o.MapFrom(message => message.Destination))
            .ForMember(responseMessage => responseMessage.CommandResponseSource, o => o.MapFrom(message => message.CommandSource))
            .ForMember(responseMessage => responseMessage.CommandResponseType, o => o.MapFrom(message => message.CommandType))
            .ForMember(responseMessage => responseMessage.CommandResponseRoute, o => o.MapFrom(message => message.CommandRoute));
    }
}
