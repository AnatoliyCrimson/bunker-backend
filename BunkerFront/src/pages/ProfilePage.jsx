import { useSelector } from 'react-redux';
import { useNavigate } from 'react-router-dom';
import { useLogoutMutation } from '../store/api';
import "../styles/pages/Profile.scss"
import { getAccessTokenFromStorage } from '../utils/tokenUtils';
import { useState } from 'react';
import ChangePassword from '../components/profile/ChangePassword';
import ChangeName from '../components/profile/ChangeName';
import ChangeEmail from '../components/profile/ChangeEmail';
import Copy from '../components/ui/Copy';
import OverlayingPopup from '../components/uikit/OverlayingPopup';
import ChangeAvatar from '../components/profile/ChangeAvatar';


function ProfilePage() {
    const { user, isAuthenticated, loading } = useSelector((state) => state.auth);
    const token = getAccessTokenFromStorage();
    
    const navigate = useNavigate();
    
    const [isProfileExitModal, setProfileExitModal] = useState(false);
    
    
    const [logout, { isLoading }] = useLogoutMutation();
    const handleLogout = async () => {
        try {
            await logout().unwrap();
            navigate('/');
        } catch (error) {
            console.error('Logout failed:', error);
        }
    };
    
    if (loading) {
        return <div>Загрузка...</div>;
    }

    if (!isAuthenticated || !user) {
        return <div className='aboba' onClick={() => {console.log(user, isAuthenticated, token)}} >Доступ запрещён. Пожалуйста, войдите в аккаунт.</div>;
    }

    return (
        <> 
            <div className="background background--main profile">
                <div className="container profile__container">
                    <h1 className="profile__title">Профиль</h1>
                    <div className="profile__header">
                        <div className="profile__avatar">
                            <ChangeAvatar userName={user.name} userAvatar={user.avatarUrl} />
                            <p className="profile__avatar-name">
                                { user.name }
                            </p>
                        </div>

                        <button 
                            className="btn btn--small"
                            onClick={() => setProfileExitModal(true)}
                        >
                            Выход
                        </button>


                        <OverlayingPopup contentClassName={"profile__modal"} onClose={() => setProfileExitModal(false)} isOpened={isProfileExitModal}>
                            Вы уверены что хотите выйти?
                            <button
                                onClick={handleLogout}
                                disabled={isLoading}
                                className="btn btn--small profile__logout-btn"
                            >
                                {isLoading ? 'Выход...' : 'Выйти'}
                            </button>
                        </OverlayingPopup>
                    </div>
                    <div className="profile__content">
                        <div className="profile__item-container name">
                            <ChangeName userName={user.name} />
                        </div>
                        <div className="profile__item-container email">
                            <ChangeEmail userEmail={user.email} />
                            
                        </div>
                        <div className="profile__item-container password">
                            <ChangePassword />
                        </div>
                    </div>
                    <div className="profile__uid">
                        <Copy
                            text={user.id}
                            containerClass={"profile__copy-container"}
                            textClass={"profile__copy-text"}
                            descrClass={""}
                        />
                        
                    </div>
                </div>

            </div>
            
        </>
    );
}

export default ProfilePage;