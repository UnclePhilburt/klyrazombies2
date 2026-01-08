var WebGLInputPlugin = {

    // Prevent Tab key default browser behavior (focus switching)
    PreventTabDefault: function() {
        document.addEventListener('keydown', function(e) {
            // Prevent Tab from switching focus
            if (e.key === 'Tab' || e.keyCode === 9) {
                e.preventDefault();
                return false;
            }
        }, true);

        console.log('[WebGLInput] Tab key default behavior disabled');
    },

    // Prevent multiple keys from default behavior
    PreventKeysDefault: function() {
        var preventedKeys = [
            9,   // Tab
            112, // F1
            113, // F2
            114, // F3
            115, // F4
            116, // F5
            117, // F6
            118, // F7
            119, // F8
            120, // F9
            121, // F10
            122, // F11
            123, // F12
            27   // Escape (prevent exiting fullscreen in some browsers)
        ];

        document.addEventListener('keydown', function(e) {
            if (preventedKeys.indexOf(e.keyCode) !== -1) {
                e.preventDefault();
                return false;
            }
        }, true);

        console.log('[WebGLInput] Browser default key behaviors disabled');
    }
};

mergeInto(LibraryManager.library, WebGLInputPlugin);
