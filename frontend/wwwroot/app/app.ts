///<reference path="../dhtmlx/dhtmlx.d.ts"/>

function appInit(): void {
    let layout = new dhtmlXLayoutObject(document.body, "2U");

    let disassembly = layout.cells("a");
    disassembly.setText("Disassembly");
    let disassemblyText: HTMLPreElement = document.getElementById("disassemblyText") as HTMLPreElement;
    disassembly.attachObject(disassemblyText);

    let symbols = layout.cells("b");
    symbols.setText("Symbols");
    symbols.setWidth(500);
    let symbolsTree = symbols.attachTreeView();
    symbolsTree.attachEvent("onclick", function (id: string): boolean {
        let r = dhx.ajax.getSync("api/assembly/instructions/" + id + "/200");
        disassemblyText.innerText = r.xmlDoc.responseText;
        return true;
    });

    let menu = layout.attachMenu();
    menu.setIconsPath("icons/");
    menu.loadStruct("layouts/menu.xml");

    let uploadUrl = "";
    let postUploadAction: Function = null;
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
        uploadInfo.attachHTMLString("<p>Please wait...</p>");
        uploadInfo.show();
        uploadInfo.center();
        uploadInfo.progressOn();

        let form = new FormData();
        let xhr = new XMLHttpRequest();

        let file = this.files[0];
        form.append('file', file, file.name);

        xhr.onload = function () {
            windowSystem.unload();
            postUploadAction();
        };

        xhr.onprogress = function (event) {
            let p = event.loaded * 100.0 / event.total;
            uploadInfo.attachHTMLString("<p>Please wait...</p><p>" + p + "%</p>");
        };

        xhr.open('POST', uploadUrl);
        xhr.send(form);
    });

    menu.attachEvent('onclick', function (id): void {
        if (id === 'ftLoadSym') {
            uploadUrl = 'api/upload/sym';
            postUploadAction = function () {
                symbolsTree.loadStruct("api/symbols");
            };
            fileInput.click();
        }
        else if (id === 'ftLoadExe') {
            uploadUrl = 'api/upload/exe';
            postUploadAction = function () {
                disassemblyText.innerHTML = "";
            };
            fileInput.click();
        }
    });

    /*let toolbar = layout.attachToolbar();
     toolbar.setIconsPath("icons/");
     toolbar.loadStruct("layouts/toolbar.xml");*/
}
