namespace ResultViewer.Application.DTOs;

public record ErrorResponseDto(string Message, string? Detail = null, string? TraceId = null);
