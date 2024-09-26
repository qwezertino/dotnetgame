window.addKeyListener = function (dotNetHelper) {
    window.addEventListener('keydown', function (event) {
        dotNetHelper.invokeMethodAsync('OnKeyPress', event.key);
    });
};

