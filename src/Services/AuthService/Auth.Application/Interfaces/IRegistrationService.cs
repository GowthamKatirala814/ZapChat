using Auth.Application.DTOs;

namespace Auth.Application.Interfaces;

public interface IRegistrationService
{
    Task<InitiateRegistrationResponseDto> InitiateRegistrationAsync(InitiateRegistrationRequestDto dto);

    Task<VerifyRegistrationOtpResponseDto> VerifyRegistrationOtpAsync(VerifyRegistrationOtpRequestDto dto);

    Task<CompleteRegistrationResponseDto> CompleteRegistrationAsync(CompleteRegistrationRequestDto dto);
}
