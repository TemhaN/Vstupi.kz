// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
if (window.location.pathname.includes('/Exam/Start')) {
    let warningCount = 0;
    window.onbeforeunload = function (e) {
        warningCount++;
        if (warningCount >= 2) {
            window.location.href = '/Exam/Blocked';
        }
        return 'Вы уверены, что хотите покинуть экзамен?';
    };
}