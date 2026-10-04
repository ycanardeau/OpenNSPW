
	// Forward Declear File


void	draw( void );

HRESULT	EnumAdapters( HWND hDlg, GUID* pSPGuid );
HRESULT	EnumServiceProviders( HWND hDlg );
VOID		SetupAddressFields( HWND hDlg );

HRESULT LaunchMultiplayerGame( HWND hDlg );

//INT_PTR CALLBACK OverrideDlgProc( HWND hDlg, UINT msg, WPARAM wParam, LPARAM lParam );
BOOL CALLBACK OverrideDlgProc(HWND hDlg, UINT Msg, WPARAM wParam, LPARAM lParam);

INT_PTR CALLBACK GreetingDlgProc( HWND hDlg, UINT msg, WPARAM wParam, LPARAM lParam );
HRESULT WINAPI DirectPlayMessageHandler( PVOID pvUserContext, DWORD dwMessageId, PVOID pMsgBuffer );

int EndApp(void);
void get_input(void);
bool InitDInput(void);

void	cnct_game_init(void);

void	make_my_rnd(void);
int	my_rnd(int	r);
int	rnd( int x);

void	cloud_in_start( void );
void	cloud_cont( void );
void		set_sprt_data( void );

BOOL	pt_in_rect(RECT *dstn_rect,int crsr_x,int crsr_y);
BOOL	pt_in_rect2(RECT *dstn_rect,int crsr_x,int crsr_y);
BOOL	pt_in_rect3(RECT *dstn_rect,int crsr_x,int crsr_y);

void	load_user_map( void );

int		plane_in_cv( int m );
int		seek_parking_no( int m );
void	set_pos_of_parking( int	m );

void	load_it2(char	*str );
void	load_it3( void );

void	unit_info_cont(void);

void	set_the_slct_unit (int m);

void	restoreAll( void );


void	cls_all_slct_unit(void);
void	cls_all_slct_unit_p2( int side );
void	draw_cmbt_area(void);


void	draw_line5(int	x1,int	y1,int	x2,int	y2, int right, int bottom , WORD cl);
void	draw_line4(int	x1,int	y1,int	x2,int	y2, int right, int bottom , COLORREF rgb);


int		seek_effect_no( void );
int		seek_fire_no( void );



BOOL	same_rect(RECT *dstn_rect, RECT *src_rect, RECT *field_rect);

int		find_out_size( int m , int n);
void		draw_map( void );

BOOL	InitDSound(void);
BOOL	InitDMusic(void);
LPDIRECTSOUNDBUFFER LoadWave(char *name);




void	cnct_decision( void );

void	chara_cont(void);

void	save_on_resume( int	type );


void go_cnct_game_setting(void);

void new_unit_arrived(int side, int new_unit_kind );
void	cnct_game_input_now( void );
void	cnct_unit_info_cont_now(void);

void		find_out( void );


// etc1
int		hit_chk(int m);
void		draw_hit_area(int m);
void	fire_now( int m, int trgt, int kind );
void	set_pos_of_dynmc( int	n );
void	chk_another_unit( double *rx,  double *ry);
void	set_pos_of_emrgncy_FT( int	m );
void	set_pos_of_attack_FT( int	m );
void	set_pos_of_emrgncy_AT( int	m );
void	set_pos_of_attack_AT( int	m );
void	set_pos_of_attack_TR1( int m );
void em_of_out_of_map(int m);
void	set_pos_of_emrgncy_SHIP( int m );
// etc2
void	cont_pos_of_take_down( int n );
void	set_pos_of_take_down(int n);
void	set_frmtn_of_ships( int s );
int		drctn_for_8(int drctn);
int	rtn_damage_pt(int	m);
//etc3
void	cont_unit_effect( int m );
void	edit_now( void );
void	cont_fire( void );

void	load_on_resume( int type );
void	set_cpu_root2( int	m );
void get_sinario_data(void);

int		set_new_unit(int	side,int	kind,double rx, double ry,double drctn);
int		set_new_unit_2(int	side,int	kind,int	type, double rx, double ry,double drctn);

void	be_dstryd( int m );
void	cnct_game_input_cont( void );

LRESULT CALLBACK MainWndProc(HWND hWnd,UINT msg,UINT wParam,LONG lParam);


void	init_apl_reg( void );
LPDIRECTDRAWSURFACE7 bitmap_surface( LPCTSTR file_name );

void	demo_func(void);

void go_cnct_game_setting(void);
void cnct_game_setting(void);

BOOL CALLBACK ChatDlgProc(HWND hWnd,UINT msg,UINT wParam,LONG lParam);

void make_map_cg( void );

int		set_new_unit_plane(int	side,int	kind,int type, int no,int planes,int arm);


BOOL	CALLBACK	IDD_OK_CANCEL_Proc(HWND hWnd,UINT msg,UINT wParam,LONG lParam);
BOOL	CALLBACK	IDD_FILE_LOAD_Proc(HWND hWnd,UINT msg,UINT wParam,LONG lParam);
BOOL	CALLBACK	IDD_FILE_SAVE_Proc(HWND hWnd,UINT msg,UINT wParam,LONG lParam);
void	my_dlg_wait( void );


// 消す可能性大のやつ
void WaveToAllPlayers(void);

void	play_snd( LPDIRECTSOUNDBUFFER	the_lpdsb, short	*the_snd, int	f);
void SoundPlayEffect( int dwFlags, int no ,double	x,	double y );

