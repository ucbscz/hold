import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Provider } from '@angular/core';
import { TestModuleMetadata } from '@angular/core/testing';
import {
  ActivatedRoute,
  convertToParamMap,
  provideRouter,
} from '@angular/router';
import { of } from 'rxjs';

const activatedRouteStub = {
  snapshot: {
    paramMap: convertToParamMap({}),
    queryParamMap: convertToParamMap({}),
    params: {},
    queryParams: {},
  },
  paramMap: of(convertToParamMap({})),
  queryParamMap: of(convertToParamMap({})),
  params: of({}),
  queryParams: of({}),
};

export function withDefaultTestingProviders(
  metadata: TestModuleMetadata = {},
): TestModuleMetadata {
  return {
    ...metadata,
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      { provide: ActivatedRoute, useValue: activatedRouteStub },
      ...((metadata.providers as Provider[]) ?? []),
    ],
  };
}
