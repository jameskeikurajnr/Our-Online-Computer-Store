// Click-to-enlarge for profile photos. Any element with data-avatar-lightbox="<url>"
// (rendered by the UserAvatar view component) opens that url in a full-screen
// overlay — works everywhere an avatar is shown (navbar, account pages) with
// no per-page wiring needed.
(function () {
    var overlay = document.getElementById('avatarLightbox');
    var overlayImg = document.getElementById('avatarLightboxImg');
    if (!overlay || !overlayImg) return;

    function openLightbox(src) {
        overlayImg.src = src;
        overlay.hidden = false;
    }

    function closeLightbox() {
        overlay.hidden = true;
        overlayImg.src = '';
    }

    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('[data-avatar-lightbox]');
        if (!trigger) return;

        // Stops the click from also triggering whatever the photo happens to be
        // sitting inside — the navbar avatar lives inside the account dropdown
        // toggle, and without this, clicking the photo would both enlarge it
        // and pop the dropdown open underneath.
        e.preventDefault();
        e.stopPropagation();
        openLightbox(trigger.getAttribute('data-avatar-lightbox'));
    });

    overlay.addEventListener('click', function (e) {
        // Only closes on a backdrop click — clicking the enlarged photo itself
        // (or the explicit close button, handled below) is what people expect
        // from a lightbox; use Escape or the × to close either way.
        if (e.target === overlay) closeLightbox();
    });

    var closeBtn = overlay.querySelector('.avatar-lightbox-close');
    if (closeBtn) closeBtn.addEventListener('click', closeLightbox);

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !overlay.hidden) closeLightbox();
    });
})();
