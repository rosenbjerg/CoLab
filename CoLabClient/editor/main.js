var Range = require("ace/range").Range;
var editor, session, ws, clickedId, clickedName, inserting = false, changingFile = false, curFid = "";
var $pos = $("#position"), $filePanel = $("#project-explorer-files");
var dirRegExp = /^[\w]+$/, fileRegExp = /^[\w\.\-]+$/;
var modelist = ace.require("ace/ext/modelist");
ace.require("ace/ext/language_tools");
editor = ace.edit("code");
session = editor.getSession();
editor.$blockScrolling = Infinity;
editor.setOptions({
    enableBasicAutocompletion: true,
    enableSnippets: true,
    enableLiveAutocompletion: true
});
editor.setTheme("ace/theme/monokai");
editor.setReadOnly(true);
$("#ace_settingsmenu, #kbshortcutmenu").css("background-color", "#616161");
session.on('change', onSessionTextChange);
session.selection.on('changeCursor', onSessionCursorChanged);
var undoManager = session.$undoManager;

function getQueryVal(name, url) {
    if (!url)
        url = window.location.href;
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

function loadFile(fid, name) {
    if (curFid == fid)
        return;
    $.get("/project/" + pid + "/" + fid, function( data ) {
        changingFile = true;
        editor.setReadOnly(false);
        session.setValue(data);
        session.setMode(modelist.getModeForPath(name).mode);
        editor.focus();
        changingFile = false;
        connectWS(fid);
        var stateObj = {foo: "bar"};
        var extension = name.substr((name.lastIndexOf('.')+1));
        history.replaceState(stateObj, name+" - CoLob", location.pathname+"?m="+extension+"&fid="+fid);
        curFid = fid;
    });
}

function loadProjectMeta(){
    $.getJSON("/project/" + pid + "/meta", function (data) {
        loadMenu(data.Files);
        $("#project-explorer-header").find("> a").text(data.Title);
    })
}

function onSessionTextChange(e) {
    if (!inserting && !changingFile)
    {
        if (e.action == "remove")
            e.lines = [];
        e.sender = sessionStorage.getItem("sessid");
        ws.send("tc" + JSON.stringify(e));
    }
}
function onSessionCursorChanged() {
    var c = editor.selection.getCursor();
    $pos.text(c.row +1 + ":" + c.column);
}

var msbuffer = "";
function connectWS(fid) {
    if (ws)
        ws.close();
    ws = new WebSocket("ws://localhost:5005" +"/" + pid + "/" + fid);
    ws.onmessage = function (msg) {
        console.log(msg);
        console.log(msg.data);
        if (msg.data.charAt(msg.data.length-1) != "}" || msg.data.charAt(2) != "{"){
            msbuffer += msg.data;
            return;
        }
        if (msbuffer != ""){
            msbuffer += msg.data;
            msg.data = msbuffer;
            msbuffer = "";
        }
        var data = JSON.parse(msg.data.substr(2));
        var msgType = msg.data.substr(0,2);
        if (msgType == "tc"){
            if (data.sender == sessionStorage.getItem("sessid"))
                return;
            inserting = true;
            if (data.action == "insert")
                editor.session.insert(data.start, data.lines.join("\n"));
            else
                editor.session.replace(new Range(data.start.row, data.start.column, data.end.row, data.end.column), "");
            inserting = false;
        }
        else if (msgType == "rf"){
            loadProjectMeta();
        }
    };
}

window.addEventListener("beforeunload", function () {
    if (ws)
        ws.close();
});

$(document).bind("mousedown", function (e) {
    if (!$(e.target).parents(".custom-menu").length > 0) {
        $(".custom-menu").hide(100);
    }
});

function formatFileMenu(rootDir, startPath) {
    var retVal = "";
    if (rootDir.Name != ""){
        startPath += "/" + rootDir.Name;
    }
    if (rootDir.Dirs){
        for (var i = 0; i < rootDir.Dirs.length; i++){
            var id = rootDir.Dirs[i].Id;
            var name = rootDir.Dirs[i].Name;
            retVal += '<li><a id="' + id + '" name="' + name + '" class="panel-dir" >' + name + '</a><ul>'+ formatFileMenu(rootDir.Dirs[i], startPath) + '</ul></li>';
        }
    }
    if (rootDir.Files){
        for (i = 0; i < rootDir.Files.length; i++){
            id = rootDir.Files[i].Id;
            name = rootDir.Files[i].Name;
            retVal += '<li><a id="' + id + '" name="' + name + '" onclick="loadFile(\'' + id + '\', \'' + name + '\')">' + name + '</a></li>'
        }
    }

    return retVal;
}

function loadMenu(files) {
    $filePanel.empty();
    $filePanel.append(formatFileMenu(files, ""));

    $filePanel.find("a").on("click", function (e) {
        if ($(this).parent().has("ul")) {
            e.preventDefault();
        }
        $(this).next('ul').slideToggle();
    });

    $("#project-explorer-header").find("> a").bind("contextmenu", function (event) {
        if (event.ctrlKey) return;
        event.preventDefault();
        clickedId = $(this).attr('id');
        clickedName = $(this).attr('name');
        $("#rootdir-rightclick").finish().toggle(100).css({
            top: event.pageY + "px",
            left: event.pageX + "px"
        });
    });

    $filePanel.find("li a:not(.panel-dir)").bind("contextmenu", function (event) {
        if (event.ctrlKey) return;
        event.preventDefault();
        clickedId = $(this).attr('id');
        clickedName = $(this).attr('name');
        $("#file-rightclick").finish().toggle(100).css({
            top: event.pageY + "px",
            left: event.pageX + "px"
        });
    });

    $filePanel.find("li a.panel-dir").bind("contextmenu", function (event) {
        if (event.ctrlKey) return;
        event.preventDefault();
        clickedId = $(this).attr('id');
        clickedName = $(this).attr('name');
        $("#dir-rightclick").finish().toggle(100).css({
            top: event.pageY + "px",
            left: event.pageX + "px"
        });
    });

}

var $dialog = $("#dialogTarget");
function openInputDialog(title, msg, val, html, checkFunc, onSuccess) {
    $dialog.text(msg + " ");
    $dialog.append(html);
    $("#dialogInput").val(val);
    $dialog.dialog({
        resizable: false,
        height: "auto",
        title: title,
        width: "auto",
        modal: true,
        buttons: {
            "OK": function() {
                var val = $("#dialogInput").val();
                if (!checkFunc(val)) return;
                if (onSuccess)
                    onSuccess(val);
                $dialog.dialog( "close" );
                $dialog.empty();
            },
            Cancel: function() {
                $dialog.dialog( "close" );
                $dialog.empty();
            }
        }
    });
}
function announceRefresh() {
    if (ws)
        ws.send("rf" + JSON.stringify({semder: sessionStorage.getItem("id")}));
}

$(".custom-menu li").click(function(){
    switch($(this).attr("data-action")) {
        case "importFile":
            openInputDialog("Import file", "", "", "<form id='dialogForm' enctype='multipart/form-data' action='/project/"+ pid +"/upload/"+ clickedId +"' method='post'  target='uploadTrg'>" +
            "<input id='dialogInput' type='file' required='required'/></form>", function (inp) {
                var startIndex = (inp.indexOf('\\') >= 0 ? inp.lastIndexOf('\\') : inp.lastIndexOf('/'));
                inp = inp.substr(startIndex + 1);
                console.log(inp);
                if (!fileRegExp.test(inp)) {
                    alert("Invalid file name: " + inp);
                    return false;
                }
                var form = document.getElementById('dialogForm');
                var form2 = $("#dialogInput")[0];
                console.log(form2.files[0]);
                var fd = new FormData();
                // fd.append()

                var xhr = new XMLHttpRequest();
                xhr.open('POST', "/project/" + pid + "/upload/" + clickedId, true);
                xhr.setRequestHeader("Content-type", "multipart/form-data");
                xhr.onload = function (ev) {
                    console.log(ev);
                };
                xhr.send(fd);

                // loadProjectMeta();
                // announceRefresh();

                return true;
            });
            break;
        case "addNewDir":
            openInputDialog("New directory", clickedName + "/", "", "<input id='dialogInput' type='text'/>", function (inp) {
                return dirRegExp.test(inp);
            }, function (filename) {
                var fullname = clickedId + "/" + filename;
                $.post("/project/" + pid + "/create/dir", fullname, function (data, status) {
                    if (data == "OK"){
                        loadProjectMeta();
                        announceRefresh();
                    }
                    else{
                        alert("Add new dir failed");
                    }
                });
            });
            break;
        case "addNewFile":
            openInputDialog("New file", clickedName + "/", "", "<input id='dialogInput' type='text'/>", function (inp) {
                return fileRegExp.test(inp);
            }, function (filename) {
                var fullname = clickedId + "/" + filename;
                $.post("/project/" + pid + "/create/file", fullname, function (data, status) {
                    if (data == "OK"){
                        loadProjectMeta();
                        announceRefresh();
                    }
                    else{
                        alert("Add new fil failed");
                    }
                });
            });
            break;
        case "rename":
            var i = clickedName.lastIndexOf("/");
            var s1 = clickedName.substr(0, i+1);
            var sr = clickedName.substr(i+1);
            openInputDialog("Rename", s1, sr, "<input id='dialogInput' type='text'/>", function (inp) {
                return fileRegExp.test(inp);
            }, function (filename) {
                $.post("/project/" + pid + "/rename", clickedId + "/" + filename , function (data, status) {
                    if (data == "OK"){
                        loadProjectMeta();
                        announceRefresh();
                    }
                    else{
                        alert("rename failed");
                    }
                });
            });
            break;
        case "delete":
            $dialog.text("Are you sure you want to delete '" + clickedName + "' ?");
            $dialog.dialog({
                resizable: false,
                title:"Delete file?",
                height: "auto",
                width: "auto",
                modal: true,
                buttons: {
                    "Yes": function() {
                        if (confirm("Deleting a directory will also delete anything in the directory. Are you sure you want to delete it?"))
                        {
                            $.post("/project/" + pid + "/delete", clickedId, function (data) {
                                if (data == "OK"){
                                    loadProjectMeta();
                                    announceRefresh();
                                }
                                else{
                                    alert("delete failed");
                                }
                            });
                        }
                        $dialog.dialog( "close" );
                        $dialog.html("");
                    },
                    Cancel: function() {
                        $dialog.dialog( "close" );
                        $dialog.html("");
                    }
                }
            });
            break;
    }
    $(".custom-menu").hide(100);
});

if (sessionStorage.getItem("sessid") == null){
    sessionStorage.setItem("sessid", uid+Math.random().toString(36).substr(2, 5))
}

function loadPageQuery() {
    var fid = getQueryVal('fid');
    var mode = getQueryVal('m');
    if (fid != null){
        loadFile(fid,"file."+mode);
    }
}
loadProjectMeta();
loadPageQuery();