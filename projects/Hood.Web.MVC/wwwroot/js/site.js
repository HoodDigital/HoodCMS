﻿if (!$.hood)
    $.hood = {};
$.hood.App.Extensions = {
    Ready: function () {
        // Init the hood js app with default settings.
        $.hood.App.Init();

        // Call the resize function.
        $.hood.App.Extensions.Resize();

        // Add further ready time functionality here.
    },
    Load: function () {
        // Add any load time functionality here.
    },
    Resize: function () {
        // Add any resize functionality here.
    }
}

// Variables for the $.hood.App
$.window = $(window),
$.wrapper = $('#wrapper'),
$.header = $('#header'),
$.headerWrap = $('#header-wrap'),
$.content = $('#content'),
$.footer = $('#footer');
var windowWidth = $.window.width()
defaultLogo = $('#logo').find('.standard-logo'),
retinaLogo = $('#logo').find('.retina-logo'),
defaultLogoWidth = defaultLogo.find('img').outerWidth(),
defaultLogoImg = defaultLogo.find('img').attr('src'),
retinaLogoImg = retinaLogo.find('img').attr('src'),
defaultDarkLogo = defaultLogo.attr('data-dark-logo'),
retinaDarkLogo = retinaLogo.attr('data-dark-logo');
$.background = $('#site-background-image');

// Initialise $.hood.App
$(document).ready($.hood.App.Extensions.Ready);
$(document).load($.hood.App.Extensions.Load);
$(window).resize($.hood.App.Resize);
$(window).resize($.hood.App.Extensions.Resize);