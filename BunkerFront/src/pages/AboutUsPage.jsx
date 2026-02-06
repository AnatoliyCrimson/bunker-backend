import { useState } from "react";
import OverlayingPopup from "../components/uikit/OverlayingPopup";
import "../styles/pages/AboutUs.scss"

function AboutUsPage() {
    const [isOpened, setOpened] = useState(false);
    


    return (
        <>
            <div className="background background--main about-us">
                <div className="container about-us__container">
                    AboutUs

                    <button onClick={() => setOpened(true)}>
                        Открыть
                    </button>
                    <OverlayingPopup contentClassName={"aabaw"} onClose={() => setOpened(false)} isOpened={isOpened}>
                        <h2>wadawd</h2>
                    </OverlayingPopup>
                </div>
            </div>
        </>
    );
}

export default AboutUsPage;