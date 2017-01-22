var Range = require("ace/range").Range;
var editor, session, ws, cmenuItemClicked, inserting = false, writing = false;
var $code = $("#code"), $pos = $("#position"), $filePanel = $("#project-explorer-files");
var dirRegExp = /^[\w]+$/, fileRegExp = /^[\w\.]+$/;

var modelist = ace.require("ace/ext/modelist");
ace.require("ace/ext/language_tools");
var code = $.get("http://localhost:5005/file/test.txt", function (data) {
    $code.text(data);
    editor = ace.edit("code");
    session = editor.getSession();
    editor.$blockScrolling = Infinity;
    session.setMode(modelist.getModeForPath("test.html").mode);
    editor.setOptions({
        enableBasicAutocompletion: true,
        enableSnippets: true,
        enableLiveAutocompletion: true
    });
    editor.setTheme("ace/theme/monokai");
    editor.focus();
    ws = new WebSocket("ws://localhost:5005/test.txt");

    session.selection.on('changeCursor', function() {
        var c = editor.selection.getCursor();
        setPosition(c.row +1, c.column);
    });

    session.on('change', function(e) {
        if (!inserting)
        {
            e.sender = sessionStorage.getItem("id");
            ws.send(JSON.stringify(e));
        }
    });

    ws.onmessage = function (msg) {
        var data = JSON.parse(msg.data);
        if (data.sender == sessionStorage.getItem("id"))
            return;
        inserting = true;
        if (data.action == "insert")
            editor.session.insert(data.start, data.lines.join("\n"));
        else
            editor.session.replace(new Range(data.start.row, data.start.column, data.end.row, data.end.column), "");
        inserting = false;
    };
});

function setPosition(l,c) {
    $pos.text(l + ":" + c);
}

window.addEventListener("beforeunload", function () {
    ws.close();
});

if (sessionStorage.getItem("id") === null){
    sessionStorage.setItem("id", Math.random());
}



// If the document is clicked somewhere
$(document).bind("mousedown", function (e) {
    if (!$(e.target).parents(".custom-menu").length > 0) {
        $(".custom-menu").hide(100);
    }
});

function addFiles(rootDir, startPath) {
    var retVal = "";
    if (rootDir.Name != ""){
        startPath += "/" + rootDir.Name;
    }
    if (rootDir.Dirs){
        for (var i = 0; i < rootDir.Dirs.length; i++){
            var id = startPath + "/" + rootDir.Dirs[i].Name;
            retVal += '<li><a name="' + id + '" class="panel-dir" >' + rootDir.Dirs[i].Name + '</a><ul>'+ addFiles(rootDir.Dirs[i], startPath) + '</ul></li>';
        }
    }
    if (rootDir.Files){
        for (i = 0; i < rootDir.Files.length; i++){
            id = startPath + "/" + rootDir.Files[i];
            retVal += '<li><a name="' + id + '" onclick="hfc(\'' + id + '\')">' + rootDir.Files[i] + '</a></li>'
        }
    }

    return retVal;
}

// Handle file click
function hfc(file) {
    if (file.charAt(0) == '/') file = file.substr(1);
    console.log("file clicked: " + file.trim("/"));
}

function loadMenu() {
    $filePanel.append(addFiles(testMenu(), ""));

    $filePanel.find("a").on("click", function (e) {
        if ($(this).parent().has("ul")) {
            e.preventDefault();
        }
        $(this).next('ul').slideToggle();
    });

    $("#project-explorer-header > a").bind("contextmenu", function (event) {
        if (event.ctrlKey) return;
        event.preventDefault();
        cmenuItemClicked = $(this).attr('name');
        $("#rootdir-rightclick").finish().toggle(100).css({
            top: event.pageY + "px",
            left: event.pageX + "px"
        });
    });

    $filePanel.find("li a:not(.panel-dir)").bind("contextmenu", function (event) {
        if (event.ctrlKey) return;
        event.preventDefault();
        cmenuItemClicked = $(this).attr('name');
        $("#file-rightclick").finish().toggle(100).css({
            top: event.pageY + "px",
            left: event.pageX + "px"
        });
    });

    $filePanel.find("li a.panel-dir").bind("contextmenu", function (event) {
        if (event.ctrlKey) return;
        event.preventDefault();
        cmenuItemClicked = $(this).attr('name');
        $("#dir-rightclick").finish().toggle(100).css({
            top: event.pageY + "px",
            left: event.pageX + "px"
        });
    });

}

function testMenu() {
    return {
        Name: "",
        Dirs: [
            {
                Name: "bin",
                Dirs: [
                    {
                        Name: "Debug",
                        Files: [
                            "somecode.o",
                            "somecode1.o",
                            "somecode2.o",
                            "somecode3.o"
                        ]
                    }
                ],
                Files: [
                    "something.bin",
                    "somethingelse.bin"
                ]
            }
        ],
        Files: [
            "Program.cs",
            "SomeHandler.cs"
        ]
    };
}

loadMenu();

var $dialog = $("#dialogTarget");
function openInputDialog(title, msg, val, checkFunc) {
    $dialog.text(msg + " ");
    $dialog.append("<input id='dialogInput' type='text'/>");
    $("#dialogInput").val(val);
    $dialog.dialog({
        resizable: false,
        height: "auto",
        title: title,
        width: "auto",
        modal: true,
        buttons: {
            "OK": function() {
                if (!checkFunc($("#dialogInput").val())) return;
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
// If the menu element is clicked
$(".custom-menu li").click(function(){
    if (cmenuItemClicked.charAt(0) == '/') cmenuItemClicked = cmenuItemClicked.substr(1);
    // This is the triggered action name
    switch($(this).attr("data-action")) {

        case "addNewDir":
            openInputDialog("New directory", cmenuItemClicked + "/", "", function (inp) {
                return dirRegExp.test(inp);
            });
            break;
        case "addNewFile":
            openInputDialog("New file", cmenuItemClicked + "/", "", function (inp) {
                return fileRegExp.test(inp);
            });
            break;
        case "rename":
            var i = cmenuItemClicked.lastIndexOf("/");
            var s1 = cmenuItemClicked.substr(0, i+1);
            var sr = cmenuItemClicked.substr(i+1);
            openInputDialog("Rename", s1, sr, function (inp) {
                return fileRegExp.test(inp);
            });
            break;
        case "delete":
            $dialog.text("Are you sure you want to delete '" + cmenuItemClicked + "' ?");
            $dialog.dialog({
            resizable: false,
            title:"Delete file?",
            height: "auto",
            width: 400,
            modal: true,
            buttons: {
                "Yes": function() {
                    //Rename file request
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

    // Hide it AFTER the action was triggered
    $(".custom-menu").hide(100);
});