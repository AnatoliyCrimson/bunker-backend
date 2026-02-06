import "../../styles/components/uikit/OverlayingPopup.scss"
import { useMount } from "../../hooks/useMount"
import Portal from "./Portal";
import { useState } from "react";

function OverlayingPopup ({ children, onClose, isOpened, contentClassName }) {
    const { mounted } = useMount({ isOpened });

    if (!(isOpened || mounted)) {
        return null;
    }

    return (
        <Portal>
            <div className="popup">
                <div className={`popup__back ${mounted && isOpened ? 'popup__back-active' : ''}`} onClick={onClose} />            
                <div className={`popup__content ${mounted && isOpened ? 'popup__content-active' : ''} ${contentClassName}`}>{children}</div> 
            </div>
        </Portal>
    );
};

export default OverlayingPopup ;