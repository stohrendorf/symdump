///<reference path="../dhtmlx/dhtmlx.d.ts"/>

function appInit(): void {
    let layout = new dhtmlXLayoutObject(document.body, "2U");

    let disassembly = layout.cells("a");
    let symbols = layout.cells("b");

    disassembly.setText("Disassembly");
    symbols.setText("Symbols");
    symbols.setWidth(500);
    let symbolsTree = symbols.attachTreeView();

    let menu = layout.attachMenu();
    menu.setIconsPath("icons/");
    menu.loadStruct("layouts/menu.xml");

    let fileInput: HTMLInputElement = document.getElementById("fileInput") as HTMLInputElement;

    fileInput.addEventListener("change", function (event) {
        event.preventDefault();

        if (this.files.length != 1)
            return;

        let windowSystem = new dhtmlXWindows();
        windowSystem.createWindow("uploadInfo", 10, 10, 400, 100);
        let uploadInfo = windowSystem.window("uploadInfo");
        uploadInfo.hideHeader();
        uploadInfo.setModal(true);
        uploadInfo.stick();
        uploadInfo.denyMove();
        uploadInfo.denyResize();
        uploadInfo.attachHTMLString("Uploading...");
        uploadInfo.show();
        uploadInfo.center();

        let form = new FormData();
        let xhr = new XMLHttpRequest();

        let file = this.files[0];
        form.append('file', file, file.name);

        xhr.onload = function () {
            windowSystem.unload();
            symbolsTree.loadStruct("api/symbols");
        };

        xhr.open('POST', 'api/upload/sym');
        xhr.send(form);
    });

    menu.attachEvent('onclick', function (id): void {
        if (id === 'ftLoadSym') {
            fileInput.click();
        }
    });

    /*let toolbar = layout.attachToolbar();
     toolbar.setIconsPath("icons/");
     toolbar.loadStruct("layouts/toolbar.xml");*/
}
