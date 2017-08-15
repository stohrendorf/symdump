///<reference path="../dhtmlx/dhtmlx.d.ts"/>
function appInit() {
    var layout = new dhtmlXLayoutObject(document.body, "2U");
    var disassembly = layout.cells("b");
    disassembly.setText("Disassembly");
    var disassemblyText = document.getElementById("disassemblyText");
    disassembly.attachObject(disassemblyText);
    var symbols = layout.cells("a");
    symbols.setText("Symbols");
    symbols.setWidth(300);
    var symbolsTree = symbols.attachTreeView();
    symbolsTree.attachEvent("onSelect", function (id, mode) {
        var address = symbolsTree.getUserData(id)["address"];
        var r = dhx.ajax.getSync("api/assembly/instructions/" + address + "/200");
        disassemblyText.innerText = r.xmlDoc.responseText;
    });
    symbolsTree.loadStruct("api/symbols"); // populate initial data if there's already a project loaded
    var menu = layout.attachMenu();
    menu.setIconsPath("icons/");
    menu.loadStruct("layouts/menu.xml");
    var uploadUrl = "";
    var postUploadAction = null;
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
        uploadInfo.attachHTMLString("<p>Please wait...</p>");
        uploadInfo.show();
        uploadInfo.center();
        uploadInfo.progressOn();
        var form = new FormData();
        var xhr = new XMLHttpRequest();
        var file = this.files[0];
        form.append('file', file, file.name);
        xhr.onload = function () {
            windowSystem.unload();
            postUploadAction();
        };
        xhr.onprogress = function (event) {
            var p = event.loaded * 100.0 / event.total;
            uploadInfo.attachHTMLString("<p>Please wait...</p><p>" + p + "%</p>");
        };
        xhr.open('POST', uploadUrl);
        xhr.send(form);
    });
    menu.attachEvent('onclick', function (id) {
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
