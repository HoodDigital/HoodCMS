﻿if (!$.hood)
    $.hood = {};
$.hood.Handlers = {
    Init: function () {
        // Click to select boxes
        $('body').on('click', '.select-text', $.hood.Handlers.SelectTextContent);
        $('body').on('click', '.btn.click-select[data-target][data-value]', $.hood.Handlers.ClickSelect);
        $('body').on('click', '.click-select.show-selected[data-target][data-value]', $.hood.Handlers.ClickSelect);
        $('body').on('click', '.click-select:not(.show-selected)[data-target][data-value]', $.hood.Handlers.ClickSelectClean);
        $('body').on('click', '.slide-link', $.hood.Handlers.SlideToAnchor);

        $('body').on('change', 'input[type=checkbox][data-input]', $.hood.Handlers.CheckboxChange);
        $('body').on('change', '.submit-on-change', $.hood.Handlers.SubmitOnChange);

        $('select[data-selected]').each($.hood.Handlers.SelectSetup);
        // date/time meta editor
        $('body').on('change', '.inline-date', $.hood.Handlers.DateChange);

        this.Uploaders.Init();
    },
    SubmitOnChange: function (e) {
        $(this).parents('form').submit();
    },
    DateChange: function (e) {
        // update the date element attached to the field's attach
        $field = $(this).parents('.hood-date').find('.date-output');
        date = $field.parents('.hood-date').find('.date-value').val();
        pattern = /^([0-9]{2})\/([0-9]{2})\/([0-9]{4})$/;
        if (!pattern.test(date))
            date = "01/01/2001";
        hour = $field.parents('.hood-date').find('.hour-value').val();
        if (!$.isNumeric(hour))
            hour = "00";
        minute = $field.parents('.hood-date').find('.minute-value').val();
        if (!$.isNumeric(minute))
            minute = "00";
        $field.val(date + " " + hour + ":" + minute + ":00");
        $field.attr("value", date + " " + hour + ":" + minute + ":00");
    },
    CheckboxChange: function (e) {
        // when i change - create an array, with any other checked of the same data-input checkboxes. and add to the data-input referenced tag.
        var items = new Array();
        $('input[data-input="' + $(this).data('input') + '"]').each(function () {
            if ($(this).is(":checked"))
                items.push($(this).val());
        });
        id = '#' + $(this).data('input');
        vals = JSON.stringify(items);
        $(id).val(vals);
    },
    SelectSetup: function () {
        sel = $(this).data('selected');
        if ($(this).data('selected') !== 'undefined' && $(this).data('selected') !== '') {
            selected = String($(this).data('selected'));
            $(this).val(selected);
        }
    },
    ClickSelect: function () {
        var $this = $(this);
        targetId = '#' + $this.data('target');
        $(targetId).val($this.data('value'));
        $('.click-select[data-target="' + $this.data('target') + '"]').each(function () { $(this).html($(this).data('temp')).removeClass('active'); });
        $('.click-select[data-target="' + $this.data('target') + '"][data-value="' + $this.data('value') + '"]').each(function () { $(this).data('temp', $(this).html()).html('Selected').addClass('active'); });
    },
    ClickSelectClean: function () {
        var $this = $(this);
        targetId = '#' + $this.data('target');
        $(targetId).val($this.data('value'));
        $('.click-select.clean[data-target="' + $this.data('target') + '"]').each(function () { $(this).removeClass('active'); });
        $('.click-select.clean[data-target="' + $this.data('target') + '"][data-value="' + $this.data('value') + '"]').each(function () { $(this).addClass('active'); });
    },
    SelectTextContent: function () {
        var $this = $(this);
        $this.select();
        // Work around Chrome's little problem
        $this.mouseup(function () {
            // Prevent further mouseup intervention
            $this.unbind("mouseup");
            return false;
        });
    },
    SlideToAnchor: function () {
        var scrollTop = $('body').scrollTop();
        var top = $($.attr(this, 'href')).offset().top;

        $('html, body').animate({
            scrollTop: top
        }, Math.abs(top - scrollTop));
        return false;
    },
    Uploaders: {
        Init: function () {
            $(".upload-progress-bar").hide();
            $.getScript('/lib/dropzone/dist/min/dropzone.min.js', $.proxy(function () {
                $('.image-uploader').each(function () {
                    $.hood.Handlers.Uploaders.SingleImage($(this).attr('id'), $(this).data('json'));
                });
                $('.gallery-uploader').each(function () {
                    $.hood.Handlers.Uploaders.Gallery($(this).attr('id'), $(this).data('json'));
                });
            }, this));
        },
        RefreshImage: function (data) {
            $('.' + data.Class).css({
                'background-image': 'url(' + data.Image + ')'
            });
            $('.' + data.Class).find('img').attr('src', data.Image);
        },
        SingleImage: function (tag, jsontag) {
            tag = '#' + tag;
            $tag = $(tag);
            Dropzone.autoDiscover = false;
            var avatarDropzone = new Dropzone(tag, {
                url: $(tag).data('url'),
                maxFiles: 1,
                paramName: 'file',
                parallelUploads: 1,
                autoProcessQueue: true, // Make sure the files aren't queued until manually added
                previewsContainer: false, // Define the container to display the previews
                clickable: tag // Define the element that should be used as click trigger to select files.
            });
            avatarDropzone.on("addedfile", function () {
            });
            // Update the total progress bar
            avatarDropzone.on("totaluploadprogress", function (progress) {
                $(".upload-progress-bar." + tag.replace('#', '') + " .progress-bar").css({ width: progress + "%" });
            });
            avatarDropzone.on("sending", function (file) {
                $(".upload-progress-bar." + tag.replace('#', '')).show();
                $($(tag).data('preview')).addClass('loading');
            });
            avatarDropzone.on("queuecomplete", function (progress) {
                $(".upload-progress-bar." + tag.replace('#', '')).hide();
            });
            avatarDropzone.on("success", function (file, response) {
                if (response.Success) {
                    if (response.Image) {
                        $(jsontag).val(JSON.stringify(response.Image));
                        $($(tag).data('preview')).css({
                            'background-image': 'url(' + response.Image.SmallUrl + ')'
                        });
                        $($(tag).data('preview')).find('img').attr('src', response.Image.SmallUrl);
                    }
                    $.hood.Alerts.Success("New image added!");
                } else {
                    $.hood.Alerts.Error("There was a problem adding the image: " + response.Error);
                }
                avatarDropzone.removeFile(file);
                $($(tag).data('preview')).removeClass('loading');
            });
        },
        Gallery: function (tag) {
            Dropzone.autoDiscover = false;

            var previewNode = document.querySelector(tag + "-template");
            previewNode.id = "";
            var previewTemplate = previewNode.parentNode.innerHTML;
            previewNode.parentNode.removeChild(previewNode);

            var galleryDropzone = new Dropzone(tag, {
                url: $(tag).data('url'),
                thumbnailWidth: 80,
                thumbnailHeight: 80,
                parallelUploads: 5,
                previewTemplate: previewTemplate,
                paramName: 'files',
                autoProcessQueue: true, // Make sure the files aren't queued until manually added
                previewsContainer: "#previews", // Define the container to display the previews
                clickable: ".fileinput-button", // Define the element that should be used as click trigger to select files.
                dictDefaultMessage: '<span><i class="fa fa-cloud-upload fa-4x"></i><br />Drag and drop files here, or simply click me!</div>',
                dictResponseError: 'Error while uploading file!'
            });
            $(tag + " .cancel").hide();

            galleryDropzone.on("addedfile", function (file) {
                $(file.previewElement.querySelector(".complete")).hide();
                $(file.previewElement.querySelector(".cancel")).show();
                $(tag + " .cancel").show();
            });

            // Update the total progress bar
            galleryDropzone.on("totaluploadprogress", function (progress) {
                document.querySelector("#total-progress .progress-bar").style.width = progress + "%";
            });

            galleryDropzone.on("sending", function (file) {
                // Show the total progress bar when upload starts
                document.querySelector("#total-progress").style.opacity = "1";
                // And disable the start button
            });

            // Hide the total progress bar when nothing's uploading anymore
            galleryDropzone.on("complete", function (file) {
                $(file.previewElement.querySelector(".cancel")).hide();
                $(file.previewElement.querySelector(".progress")).hide();
                $(file.previewElement.querySelector(".complete")).show();
                $.hood.Inline.Refresh('.gallery');
            });

            // Hide the total progress bar when nothing's uploading anymore
            galleryDropzone.on("queuecomplete", function (progress) {
                document.querySelector("#total-progress").style.opacity = "0";
                $(tag + " .cancel").hide();
            });

            galleryDropzone.on("success", function (file, response) {
                $.hood.Inline.Refresh('.gallery');
                if (response.Success) {
                    $.hood.Alerts.Success("New images added!");
                } else {
                    $.hood.Alerts.Error("There was a problem adding the profile image: " + response.Error);
                }
            });

            // Setup the buttons for all transfers
            // The "add files" button doesn't need to be setup because the config
            // `clickable` has already been specified.
            document.querySelector(".actions .cancel").onclick = function () {
                galleryDropzone.removeAllFiles(true);
            };
        }
    }
};
$.hood.Handlers.Init();