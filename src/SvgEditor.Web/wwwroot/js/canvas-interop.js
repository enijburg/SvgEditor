window.svgEditorCanvas = (function () {
    let dotNetRef = null;
    let svgElement = null;
    let isDragging = false;
    let lastSvgPoint = null;

    function getSvgPoint(evt) {
        const pt = svgElement.createSVGPoint();
        pt.x = evt.clientX;
        pt.y = evt.clientY;
        const ctm = svgElement.getScreenCTM();
        if (!ctm) return null;
        return pt.matrixTransform(ctm.inverse());
    }

    function onMouseDown(evt) {
        let target = evt.target;
        // Walk up from the click target to find the nearest element with a data-element-id.
        // Elements inside raw markup (e.g. <defs> children) won't have one,
        // so we skip them until we reach a managed element or the SVG root.
        while (target && target !== svgElement) {
            if (target.getAttribute && target.getAttribute('data-element-id')) break;
            target = target.parentElement;
        }
        const elementId = target && target !== svgElement
            ? target.getAttribute('data-element-id')
            : null;
        if (!elementId) return;
        evt.preventDefault();
        isDragging = true;
        lastSvgPoint = getSvgPoint(evt);
        dotNetRef.invokeMethodAsync('OnElementMouseDown', elementId,
            lastSvgPoint ? lastSvgPoint.x : 0,
            lastSvgPoint ? lastSvgPoint.y : 0);
    }

    function onMouseMove(evt) {
        if (!isDragging || !lastSvgPoint) return;
        evt.preventDefault();
        const pt = getSvgPoint(evt);
        if (!pt) return;
        const dx = pt.x - lastSvgPoint.x;
        const dy = pt.y - lastSvgPoint.y;
        lastSvgPoint = pt;
        dotNetRef.invokeMethodAsync('OnMouseMove', dx, dy);
    }

    function onMouseUp(evt) {
        if (!isDragging) return;
        isDragging = false;
        lastSvgPoint = null;
        dotNetRef.invokeMethodAsync('OnMouseUp');
    }

    return {
        initCanvas: function (ref, svg) {
            dotNetRef = ref;
            svgElement = svg;
            svg.addEventListener('mousedown', onMouseDown);
            window.addEventListener('mousemove', onMouseMove);
            window.addEventListener('mouseup', onMouseUp);
        },
        dispose: function () {
            if (svgElement) {
                svgElement.removeEventListener('mousedown', onMouseDown);
                svgElement = null;
            }
            window.removeEventListener('mousemove', onMouseMove);
            window.removeEventListener('mouseup', onMouseUp);
            dotNetRef = null;
            isDragging = false;
            lastSvgPoint = null;
        }
    };
})();
