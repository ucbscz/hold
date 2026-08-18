import { TablePaginationComponent } from './table-pagination.component';

describe('TablePaginationComponent', () => {
  let component: TablePaginationComponent;

  beforeEach(() => {
    component = new TablePaginationComponent();
    component.totalItems = 120;
    component.pageSize = 10;
  });

  it('keeps the current page centered in the visible range', () => {
    component.page = 6;

    expect(component.visiblePages).toEqual([4, 5, 6, 7, 8]);
  });

  it('keeps five valid pages at the beginning and end', () => {
    component.page = 1;
    expect(component.visiblePages).toEqual([1, 2, 3, 4, 5]);

    component.page = 12;
    expect(component.visiblePages).toEqual([8, 9, 10, 11, 12]);
  });

  it('clamps navigation to the available page range', () => {
    spyOn(component.pageChange, 'emit');

    component.goTo(20);

    expect(component.pageChange.emit).toHaveBeenCalledOnceWith(12);
  });
});
