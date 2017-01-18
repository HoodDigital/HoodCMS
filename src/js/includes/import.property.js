if (!$.hood)
    $.hood = {};
$.hood.Import = {
    Property: {
        UpdateInterval: -1,
        Init: function () {
            $.hood.Import.Property.Update();
            $('#runUpdate').click(function () {
                //$(this).addLoader();
                $.ajax({
                    url: "/admin/property/import/ftp/start/",
                    type: "POST",
                    error: function (jqXHR, textStatus, errorThrown) {
                        $.hood.Import.Property.View.ShowError("There was an error, " + jqXHR + "<br />" + textStatus + "<br />" + errorThrown);
                    },
                    success: function (result) {
                        $.hood.Import.Property.Update();
                    },
                    complete: function () {
                        //$(this).removeLoader();
                    }
                });
            });
            $('#cancel').click(function () {
                //$(this).addLoader();
                $.ajax({
                    url: "/admin/property/import/ftp/cancel/",
                    type: "POST",
                    error: function (jqXHR, textStatus, errorThrown) {
                        $.hood.Import.Property.View.ShowError("There was an error, " + jqXHR + "<br />" + textStatus + "<br />" + errorThrown);
                    },
                    success: function (result) {
                        $.hood.Import.Property.Update();
                    },
                    complete: function () {
                        //$(this).removeLoader();
                    }
                });
            });
        },
        Update: function () {
            $.ajax({
                url: "/admin/property/import/ftp/status/",
                type: "POST",
                error: function (jqXHR, textStatus, errorThrown) {
                    $.hood.Import.Property.View.ShowError("There was an error, " + jqXHR + "<br />" + textStatus + "<br />" + errorThrown);
                },
                success: function (result) {
                    if (result.Importer.Running) {
                        $.hood.Import.Property.View.ShowInfo();
                        clearInterval($.hood.Import.Property.UpdateInterval);
                        $.hood.Import.Property.UpdateInterval = setTimeout($.hood.Import.Property.Update, 250);
                    } else {
                        clearInterval($.hood.Import.Property.UpdateInterval);
                        $.hood.Import.Property.View.HideInfo();
                    }
                    $('.tp').html(result.Importer.Total);
                    $('#pu').html(result.Importer.Updated);
                    $('#pa').html(result.Importer.Added);
                    $('#pp').html(result.Importer.Processed);
                    $('#pd').html(result.Importer.Deleted);
                    $('#ToAdd').html(result.Importer.ToAdd);
                    $('#ToUpdate').html(result.Importer.ToUpdate);
                    $('#ToDelete').html(result.Importer.ToDelete);
                    $('#pt').html(result.Importer.StatusMessage);
                    $('#fp').html(Math.round(result.Ftp.Complete * 100) / 100);
                    $('#ft').html(result.Ftp.StatusMessage);
                    $('.pc').html(Math.round(result.Importer.Complete * 100) / 100);
                    $('#progressbar').css({
                        width: result.Importer.Complete + "%"
                    });
                }
            });
        },
        View: {
            HideInfo: function () {
                $('#runUpdate').removeAttr('disabled');
                $('#cancel').attr('disabled', 'disabled');
                $('#update-progress').hide();
            },
            ShowInfo: function () {
                $('#cancel').removeAttr('disabled');
                $('#runUpdate').attr('disabled', 'disabled');
                $('#update-progress').show();
            },
            ShowError: function (string) {
                $('#error').html(string).addClass('alert').addClass('alert-danger').addClass('m-t-lg');
            }
        }
    }
}
$.hood.Import.Property.Init();