﻿import { Alerts } from "./Alerts";
import { Inline } from "./Inline";
import { Response } from "./Response";

export interface ValidatorOptions {
    errorAlert?: string;

    /**
     * Called before the submit of the form data, you can return the data to be serialised.
     */
    onSubmit?: (sender: Validator) => void;

    /**
     * Called when submit is complete.
     */
    onComplete?: (data: Response, sender?: Validator) => void;

    /**
     * Called when an error occurs.
     */
    onError?: (jqXHR: any, textStatus: any, errorThrown: any) => void;

    serializationFunction?: () => string; 

}

export class Validator {
    element: HTMLFormElement;
    options: ValidatorOptions = {
        errorAlert: 'There are errors, please check the form.'
    };

    /**
      * @param element The datalist element. The element must have a data-url attribute to connect to a feed.
      */
    constructor(element: HTMLFormElement, options: ValidatorOptions) {

        this.element = element;
        if (!this.element) {
            return;
        }

        this.options.serializationFunction = function (this: Validator) {
            let rtn = $(this.element).serialize();
            return rtn;
        }.bind(this);

        this.options = { ...this.options, ...options };

        this.element.addEventListener('submit', function (this: Validator, e: Event) {
            e.preventDefault();
            e.stopImmediatePropagation();
            this.submitForm();
        }.bind(this));
    }

    submitForm() {
        this.element.classList.add('was-validated');
        if (this.element.checkValidity()) {

            this.element.classList.add('loading');

            let checkboxes = this.element.querySelector('input[type=checkbox]');
            if (checkboxes) {
                Array.prototype.slice.call(checkboxes)
                    .forEach(function (checkbox: HTMLInputElement) {
                        if ($(this).is(':checked')) {
                            $(this).val('true');
                        }
                    });
            }

            if (this.options.onSubmit) {
                this.options.onSubmit(this);
            }

            let formData = this.options.serializationFunction();

            $.post(this.element.action, formData, function (this: Validator, data: any) {
                if (this.options.onComplete) {
                    this.options.onComplete(data, this);
                }
            }.bind(this))
                .fail(this.options.onError ?? Inline.handleError);

        } else {

            if (this.options.errorAlert) {
                Alerts.error(this.options.errorAlert, null, 5000);
            }
        }

    }
}