import { ApiResponse } from './api-response';

export function extractApiValue<T>(
  response: ApiResponse<T> | T,
  fallback: T,
): T {
  if (isApiResponse(response)) {
    return response.Value ?? response.value ?? fallback;
  }

  return response ?? fallback;
}

function isApiResponse<T>(
  response: ApiResponse<T> | T,
): response is ApiResponse<T> {
  return (
    typeof response === 'object' &&
    response !== null &&
    ('Value' in response || 'value' in response)
  );
}
