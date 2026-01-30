import { useSelector } from 'react-redux';
import { useNavigate } from 'react-router-dom';
import { useLogoutMutation, useUploadAvatarMutation } from '../store/api';
import "../styles/pages/Profile.scss"
import { getAccessTokenFromStorage } from '../utils/tokenUtils';
import { useState } from 'react';
import Avatar from '../components/ui/Avatar';
import ChangePassword from '../components/profile/ChangePassword';
import ChangeName from '../components/profile/ChangeName';
import ChangeEmail from '../components/profile/ChangeEmail';
import Copy from '../components/ui/Copy';


function ProfilePage() {
    const { user, isAuthenticated, loading } = useSelector((state) => state.auth);
    const token = getAccessTokenFromStorage();
    
    const navigate = useNavigate();
    const [avatarError, setAvatarError] = useState('');
    
    
    const [logout, { isLoading }] = useLogoutMutation();
    const handleLogout = async () => {
        try {
            await logout().unwrap();
            navigate('/');
        } catch (error) {
            console.error('Logout failed:', error);
        }
    };

    const [uploadAvatar, {isLoading: avatarLoading }] = useUploadAvatarMutation();
    const handleUploadAvatar = async (e) => {
        if (avatarError) setAvatarError('');
        
        const file = e.target.files[0];
        console.log(file);
        if (!file) {
            setAvatarError("Проблема с изображением, возможно файл поврежден")
            return
        };

        if (file.size > 5 * 1024 * 1024) {
            setAvatarError("Файл слишком большой. Максимум 5 МБ")
            return;
        }

        try {
            await uploadAvatar(file).unwrap();
            e.target.value = null;
        } catch (error) {
            console.error('avatar upload failed:', error);
            setAvatarError("Не удалось загрузить аватар")
        }
    }

    
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
                            <label className={"profile__avatar-label " + (avatarLoading ? "profile__avatar-label--dis" : "")}>
                                <div className='profile__avatar-edit'>
                                    {avatarLoading ? (
                                        <>
                                            Загрузка...
                                        </>
                                    ) : (
                                        <>
                                            <svg width="80" height="81" viewBox="0 0 80 81" fill="none" xmlns="http://www.w3.org/2000/svg">
                                                <path d="M1.52575 60.4379L50.3569 10.7298L69.3394 29.3774L20.5082 79.0855M1.52575 60.4379L2.2874 78.6481L20.5082 79.0855M1.52575 60.4379L20.5082 79.0855M54.1761 6.84198L73.1586 25.4896L77.7962 20.7687L58.8137 2.12109L54.1761 6.84198Z" stroke="#939393" strokeWidth="3"/>
                                            </svg>                                
                                            
                                        </>
                                    )}
                                    
                                </div>
                                <input disabled={avatarLoading} onChange={handleUploadAvatar} type="file" accept='image/png, image/jpeg, image/jpg' />
                                <Avatar 
                                    avatarUrl={user.avatarUrl}
                                    name={user.name}
                                    className={"profile__avatar-component"}
                                />
                            </label>
                            {
                                avatarError && (
                                    <>
                                        <p className="profile__avatar-error">
                                            { avatarError } 
                                        </p>
                                    </>
                                )
                            }
                            <p className="profile__avatar-name">
                                { user.name }
                            </p>
                        </div>
                        <button
                                onClick={handleLogout}
                                disabled={isLoading}
                                className="btn btn--small profile__logout-btn"
                            >
                            {isLoading ? 'Выход...' : 'Выйти'}
                        </button>
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