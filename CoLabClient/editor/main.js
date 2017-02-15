var Range = require("ace/range").Range;
var editor, session, ws, clickedId, clickedName, inserting = false, changingFile = false;
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

function loadFile(fid, name) {
    if (ws)
        ws.close();
    session.removeAllListeners('change');
    $.get("http://localhost:5005" + "/project/" + pid + "/" + fid, function( data ) {
        changingFile = true;
        editor.setReadOnly(false);
        session.setMode(modelist.getModeForPath(name).mode);
        editor.session.setValue(data);
        editor.focus();
        changingFile = false;
        connectWS(fid);
        var stateObj = {foo: "bar"};
        history.replaceState(stateObj, name + " - CoLob", location.pathname + "?fid="+fid);
    });
}

function loadProjectMeta(){
    $.getJSON("http://localhost:5005" + "/project/" + pid + "/meta", function (data) {
        loadMenu(data.Files);
        $("#project-explorer-header").find("> a").text(data.Title);
    })
}

function connectWS(fid) {
    ws = new WebSocket("ws://localhost:5005" +"/" + pid + "/" + fid);

    session.selection.on('changeCursor', function() {
        var c = editor.selection.getCursor();
        setPosition(c.row +1, c.column);
    });
    session.on('change', function(e) {
        if (!inserting && !changingFile)
        {
            if (e.action == "remove")
                e.lines = [];
            e.sender = uid;
            ws.send("tc" + JSON.stringify(e));
            console.log(" change sent");
        }
    });
    ws.onmessage = function (msg) {
        var data = JSON.parse(msg.data.substr(2));
        var msgType = msg.data.substr(0,2);
        if (msgType == "tc"){
            if (data.sender == uid)
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
    ws.close();
});

// If the document is clicked somewhere
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
            openInputDialog("Import file", "", "", "<form id='dialogForm' enctype='multipart/form-data' action='http://localhost:5005/project/"+ pid +"/upload/"+ clickedId +"' method='post'  target='uploadTrg'>" +
            "<input id='dialogInput' type='file' required='required'/></form>", function (inp) {
                var startIndex = (inp.indexOf('\\') >= 0 ? inp.lastIndexOf('\\') : inp.lastIndexOf('/'));
                inp = inp.substr(startIndex + 1);
                console.log(inp);
                if (!fileRegExp.test(inp)) {
                    alert("Invalid file name: " + inp);
                    return false;
                }
                var form = document.getElementById('dialogForm');
                console.log(form);
                var fd = new FormData(form);

                var xhr = new XMLHttpRequest();
                xhr.open('POST', "http://localhost:5005" + "/project/" + pid + "/upload/" + clickedId, true);
                xhr.setRequestHeader("Content-type", "multipart/form-data");
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
                $.post("http://localhost:5005" + "/project/" + pid + "/create/dir", fullname, function (data, status) {
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
                $.post("http://localhost:5005" + "/project/" + pid + "/create/file", fullname, function (data, status) {
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
                $.post("http://localhost:5005" + "/project/" + pid + "/rename", clickedId + "/" + filename , function (data, status) {
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
                            $.post("http://localhost:5005" + "/project/" + pid + "/delete", clickedId, function (data) {
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

function setPosition(l,c) {
    $pos.text(l + ":" + c);
}


loadProjectMeta();