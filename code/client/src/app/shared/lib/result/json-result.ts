import { Result } from 'ts-results-es';

export function parseJsonResult<T>(jsonValue: string): Result<T, unknown> {
  return Result.wrap(() => JSON.parse(jsonValue) as T);
}
