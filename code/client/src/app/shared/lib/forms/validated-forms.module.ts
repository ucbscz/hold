import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FieldErrorsDirective } from './field-errors.directive';
import { FieldRulesDirective } from './field-rules.directive';

@NgModule({
  imports: [FormsModule, FieldErrorsDirective, FieldRulesDirective],
  exports: [FormsModule, FieldErrorsDirective, FieldRulesDirective],
})
export class ValidatedFormsModule {}
