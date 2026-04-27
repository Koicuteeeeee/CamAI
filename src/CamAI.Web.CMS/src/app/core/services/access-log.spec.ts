import { TestBed } from '@angular/core/testing';

import { AccessLog } from './access-log';

describe('AccessLog', () => {
  let service: AccessLog;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AccessLog);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
