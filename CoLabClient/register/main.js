var $form = $("form"), $status = $("#status");
var $fn = $("#fn"), $ln = $("#ln"), $co = $("#co"), $em = $("#em");
var $pw1 = $("#pw1"), $pw2 = $("#pw2"), $dt = $("#dt");
var $ci = $("#ci"), $dn = $("#dn");
var captchaContainer = null;
var loadCaptcha = function() {
    captchaContainer = grecaptcha.render('recaptcha', {
        'sitekey' : '6LeruxQUAAAAAK9TX40UB9pbaaZUi_3D1-vISL5p',
        'theme' : 'dark',
        'callback' : function(response) {
            $form.validate();
            $form.submit();

        }
    });
};
$form.submit(function (ev) {
    ev.preventDefault();
    $form.find("input, select").attr("disabled", true);
    if ($co.val() == "NONE"){
        $co[0].setCustomValidity("You must select a country");
        return;
    }
    if ($pw1.val() != $pw2.val()){
        $pw2[0].setCustomValidity("Passwords do not match");
        return;
        var d = {
            fn: $fn.val(),
            ln: $ln.val(),
            dn: $dn.val(),
            ci: $ci.val(),
            co: $co.val(),
            dt: $dt.val(),
            em: $em.val(),
            pw: $pw1.val(),
            rc: response
        };

        $.ajax({
            url: "/register",
            type:"POST",
            data:JSON.stringify(d),
            contentType:"application/json; charset=utf-8",
            success: function(data, status){
                console.log(data);
                if (data == "OK"){
                    $status.text("Registered");
                    window.location = "/login";
                }
                else {
                    $status.text("Email already in use");
                    $form.find("input, select").attr("disabled", false);
                }
            }
        });
    }
});
