///<reference path="../dhtmlx/dhtmlx.d.ts"/>
function appInit() {
    var layout = new dhtmlXLayoutObject(document.body, "2U");
    var disassembly = layout.cells("a");
    var symbols = layout.cells("b");
    disassembly.setText("Disassembly");
    symbols.setText("Symbols");
    symbols.setWidth(500);
    var symbolsTree = symbols.attachTreeView();
    var menu = layout.attachMenu();
    menu.setIconsPath("icons/");
    menu.loadStruct("layouts/menu.xml");
    var fileInput = document.getElementById("fileInput");
    fileInput.addEventListener("change", function (event) {
        event.preventDefault();
        if (this.files.length != 1)
            return;
        var windowSystem = new dhtmlXWindows();
        windowSystem.createWindow("uploadInfo", 10, 10, 400, 100);
        var uploadInfo = windowSystem.window("uploadInfo");
        uploadInfo.hideHeader();
        uploadInfo.setModal(true);
        uploadInfo.stick();
        uploadInfo.denyMove();
        uploadInfo.denyResize();
        uploadInfo.attachHTMLString("Uploading...");
        uploadInfo.show();
        uploadInfo.center();
        var form = new FormData();
        var xhr = new XMLHttpRequest();
        var file = this.files[0];
        form.append('file', file, file.name);
        xhr.onload = function () {
            windowSystem.unload();
            symbolsTree.loadStruct("api/symbols");
        };
        xhr.open('POST', 'api/upload/sym');
        xhr.send(form);
    });
    menu.attachEvent('onclick', function (id) {
        if (id === 'ftLoadSym') {
            fileInput.click();
        }
    });
    /*let toolbar = layout.attachToolbar();
     toolbar.setIconsPath("icons/");
     toolbar.loadStruct("layouts/toolbar.xml");*/
}
