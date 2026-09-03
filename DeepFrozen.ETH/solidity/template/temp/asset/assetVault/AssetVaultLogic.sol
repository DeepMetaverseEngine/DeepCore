// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./AssetVaultLayout.sol";
import "../nameServiceRef/NameServiceRefLogic.sol";
import "../holders/HERC721HolderLogic.sol";
import "../holders/HERC1155HolderLogic.sol";
import "./AssetVaultInterface.sol";

import "../erc20improved/HERC20IMInterface.sol";
import "../erc1155improved/HERC1155IMInterface.sol";
import "../erc721improved/HERC721IMInterface.sol";

import "./AssetVaultType.sol";

abstract contract AssetVaultLogic is AssetVaultLayout, NameServiceRefLogic, HERC721HolderLogic, HERC1155HolderLogic, AssetVaultInterface {

    using SafeMath for uint256;
    using SafeERC20 for IERC20;

    /*
    make sure the the manager has "sudo" permit to mint token
    */
    function _mintErc20(
        bytes32 erc20TokenName,
        address owner,
        uint256 amount
    ) internal {
        address erc20Token = ns().getSingleSafe(erc20TokenName);
        HERC20IMInterface(erc20Token).mintSudo(owner, amount);
        emit MintErc20(erc20Token, owner, amount);
    }

    //no map here

    //no bind here

    function _depositErc20(
        bytes32 erc20TokenName,
        address owner,
        uint256 amount
    ) internal {
        address erc20Token = ns().getSingleSafe(erc20TokenName);

        uint256 depositStrategy = _erc20DepositStrategy[erc20Token];
        if (depositStrategy == AssetVaultType.ERC20_DEPOSIT_STRATEGY_BURN) {

            HERC20IMInterface(erc20Token).burnSudo(owner, amount);

        } else if (depositStrategy == AssetVaultType.ERC20_DEPOSIT_STRATEGY_ADDRESS) {

            address holder = _erc20DepositStrategyAddress[erc20Token];
            uint256 temp = IERC20(erc20Token).balanceOf(holder);
            IERC20(erc20Token).safeTransferFrom(owner, holder, amount);
            amount = IERC20(erc20Token).balanceOf(holder).sub(temp);

        } else if (depositStrategy == AssetVaultType.ERC20_DEPOSIT_STRATEGY_STORE) {

            uint256 temp = IERC20(erc20Token).balanceOf(address(this));
            IERC20(erc20Token).safeTransferFrom(owner, address(this), amount);
            amount = IERC20(erc20Token).balanceOf(address(this)).sub(temp);

        }

        emit DepositErc20(erc20Token, owner, owner, amount);
    }

    function _withdrawErc20(
        bytes32 erc20TokenName,
        address owner,
        uint256 amount,
        uint256 traceId
    ) internal {
        address erc20Token = ns().getSingleSafe(erc20TokenName);

        uint256 withdrawStrategy = _erc20WithdrawStrategy[erc20Token];
        if (withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_MINT) {

            HERC20IMInterface(erc20Token).mintSudo(owner, amount);

        } else if (withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_ADDRESS) {

            address holder = _erc20WithdrawStrategyAddress[erc20Token];
            IERC20(erc20Token).safeTransferFrom(holder, owner, amount);

        } else if (
            withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_STORE_MINT ||
            withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_STORE_REVERT
        ) {
            uint256 bal = IERC20(erc20Token).balanceOf(address(this));
            if (bal < amount) {

                if (withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_STORE_MINT) {
                    //mint the remaining
                    HERC20IMInterface(erc20Token).mintSudo(owner, amount.sub(bal));
                } else {
                    revert("insufficient erc20 token for withdraw");
                }
            }
            IERC20(erc20Token).safeTransfer(owner, bal);
        }

        emit WithdrawErc20(erc20Token, owner, amount, traceId);
    }

    //==========================================================================================

    function _mintErc1155(
        bytes32 erc1155TokenName,
        address owner,
        uint256 tokenId,
        uint256 amount
    ) internal {
        address erc1155Token = ns().getSingleSafe(erc1155TokenName);
        HERC1155IMInterface(erc1155Token).mintSudo(owner, tokenId, amount);
        emit MintErc1155(erc1155Token, owner, tokenId, amount);
    }

    //no map here

    //no bind here

    function _depositErc1155(
        bytes32 erc1155TokenName,
        address owner,
        uint256 tokenId,
        uint256 amount
    ) internal {
        address erc1155Token = ns().getSingleSafe(erc1155TokenName);

        uint256 depositStrategy = _erc1155DepositStrategy[erc1155Token];
        if (depositStrategy == AssetVaultType.ERC1155_DEPOSIT_STRATEGY_BURN) {

            HERC1155IMInterface(erc1155Token).burnSudo(owner, tokenId, amount);

        } else if (depositStrategy == AssetVaultType.ERC1155_DEPOSIT_STRATEGY_ADDRESS) {

            address holder = _erc1155DepositStrategyAddress[erc1155Token];
            uint256 temp = IERC1155(erc1155Token).balanceOf(holder, tokenId);
            IERC1155(erc1155Token).safeTransferFrom(owner, holder, tokenId, amount, "");
            amount = IERC1155(erc1155Token).balanceOf(holder, tokenId).sub(temp);

        } else if (depositStrategy == AssetVaultType.ERC1155_DEPOSIT_STRATEGY_STORE) {

            uint256 temp = IERC1155(erc1155Token).balanceOf(address(this), tokenId);
            IERC1155(erc1155Token).safeTransferFrom(owner, address(this), tokenId, amount, "");
            amount = IERC1155(erc1155Token).balanceOf(address(this), tokenId).sub(temp);

        }

        emit DepositErc1155(erc1155Token, owner, owner, tokenId, amount);
    }

    function _withdrawErc1155(
        bytes32 erc1155TokenName,
        address owner,
        uint256 tokenId,
        uint256 amount,
        uint256 traceId
    ) internal {
        address erc1155Token = ns().getSingleSafe(erc1155TokenName);

        uint256 withdrawStrategy = _erc1155WithdrawStrategy[erc1155Token];
        if (withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_MINT) {
            HERC1155IMInterface(erc1155Token).mintSudo(owner, tokenId, amount);
        } else if (withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_ADDRESS) {
            address holder = _erc1155WithdrawStrategyAddress[erc1155Token];
            IERC1155(erc1155Token).safeTransferFrom(holder, owner, tokenId, amount, "");
        } else if (
            withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_STORE_MINT ||
            withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_STORE_REVERT
        ) {
            uint256 bal = IERC1155(erc1155Token).balanceOf(address(this), tokenId);
            if (bal < amount) {

                if (withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_STORE_MINT) {
                    //mint the remaining
                    HERC1155IMInterface(erc1155Token).mintSudo(owner, tokenId, amount.sub(bal));
                } else {
                    revert("insufficient erc1155 token for withdraw");
                }
            }
            IERC1155(erc1155Token).safeTransferFrom(address(this), owner, tokenId, bal, "");
        }
        emit WithdrawErc1155(erc1155Token, owner, tokenId, amount, traceId);
    }
    //==========================================================================================

    //just sync attribute while online
    function _syncErc721(
        bytes32 erc721TokenName,
        uint256 tokenId,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Data,
        bytes32[] memory bytes32Data,
        address[] memory addressData,
        bytes[] memory bytesData
    ) internal {
        address erc721Token = ns().getSingleSafe(erc721TokenName);
        _setAttributeErc721_(erc721Token, tokenId, attributeNames, uint256Data, bytes32Data, addressData, bytesData);
        emit SyncErc721(erc721Token, tokenId, attributeNames, uint256Data, bytes32Data, addressData, bytesData);
    }



    /*
    make sure the the manager has "sudo" permit to mint token
    */
    //mint 721 online to OWNER
    function _mintErc721Lite(
        bytes32 erc721TokenName,
        address owner
    ) internal returns (uint256 tokenId) {
        tokenId = _mintErc721(
            erc721TokenName,
            owner,
            new bytes32[](0),
            new uint256[](0),
            new bytes32[](0),
            new address[](0),
            new bytes[](0)
        );
        return tokenId;
    }

    function _mintErc721(
        bytes32 erc721TokenName,
        address owner,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Data,
        bytes32[] memory bytes32Data,
        address[] memory addressData,
        bytes[] memory bytesData
    ) internal returns (uint256 tokenId) {

        address erc721Token = ns().getSingleSafe(erc721TokenName);
        tokenId = HERC721IMInterface(erc721Token).mintSudo(owner, 0);

        _setAttributeErc721_(erc721Token, tokenId, attributeNames, uint256Data, bytes32Data, addressData, bytesData);

        emit MintErc721(erc721Token, owner, tokenId, attributeNames, uint256Data, bytes32Data, addressData, bytesData);

        return tokenId;
    }

    //!!!!!!!!never call this while the token is offline, only know if you need it, call upload instead!!!!!!!!!!!
    //mint offline 721 in VAULT,
    function _mapErc721Lite(
        bytes32 erc721TokenName,
        uint256 tokenId
    ) internal {
        _mapErc721(
            erc721TokenName,
            tokenId,
            new bytes32[](0),
            new uint256[](0),
            new bytes32[](0),
            new address[](0),
            new bytes[](0)
        );
    }

    //!!!!!!!!never call this while the token is offline, only know if you need it, call upload instead!!!!!!!!!!!
    //mint offline 721 in VAULT,
    function _mapErc721(
        bytes32 erc721TokenName,
        uint256 tokenId,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Data,
        bytes32[] memory bytes32Data,
        address[] memory addressData,
        bytes[] memory bytesData
    ) internal {

        address erc721Token = ns().getSingleSafe(erc721TokenName);

        (uint256 from, uint256 to) = HERC721IMInterface(erc721Token).tokenIdMapRange();
        require((from <= tokenId && tokenId < to) || (from == 0 && to == 0), "_mapErc721, tokenId should be in [from,to) of tokenIdMapRange");

        tokenId = HERC721IMInterface(erc721Token).mintSudo(address(this), tokenId);

        _setAttributeErc721_(erc721Token, tokenId, attributeNames, uint256Data, bytes32Data, addressData, bytesData);

        emit MapErc721(erc721Token, tokenId, attributeNames, uint256Data, bytes32Data, addressData, bytesData);

    }

    //burn online token
    function _burnErc721(
        bytes32 erc721TokenName,
        uint256 tokenId
    ) internal {
        address erc721Token = ns().getSingleSafe(erc721TokenName);

        require(HERC721IMInterface(erc721Token).exists(tokenId), "_burnErc721, tokenId does not exist");
        require(HERC721IMInterface(erc721Token).ownerOf(tokenId) != address(this), "_burnErc721, tokenId belongs to AssetVault");

        HERC721IMInterface(erc721Token).burnSudo(tokenId);
        emit BurnErc721(erc721Token, tokenId);
    }

    function _depositErc721(
        bytes32 erc721TokenName,
        address owner,
        uint256 tokenId
    ) internal {
        address erc721Token = ns().getSingleSafe(erc721TokenName);

        require(tokenId != 0, "_depositErc721, tokenId is zero");

        IERC721(erc721Token).safeTransferFrom(owner, address(this), tokenId);
        emit DepositErc721(erc721Token, owner, owner, tokenId);
    }


    //withdraw tokenId from vault
    function _withdrawErc721Lite(
        bytes32 erc721TokenName,
        address owner,
        uint256 tokenId
    ) internal {
        _withdrawErc721(
            erc721TokenName,
            owner,
            tokenId,
            new bytes32[](0),
            new uint256[](0),
            new bytes32[](0),
            new address[](0),
            new bytes[](0)
        );
    }


    //withdraw tokenId from vault
    function _withdrawErc721(
        bytes32 erc721TokenName,
        address owner,
        uint256 tokenId,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Data,
        bytes32[] memory bytes32Data,
        address[] memory addressData,
        bytes[] memory bytesData
    ) internal {
        address erc721Token = ns().getSingleSafe(erc721TokenName);

        require(tokenId != 0, "_withdrawErc721, tokenId is 0, maybe should map it before");

        require(IERC721(erc721Token).ownerOf(tokenId) == address(this), "the token is not in vault");

        IERC721(erc721Token).safeTransferFrom(address(this), owner, tokenId);

        emit WithdrawErc721(erc721Token, owner, tokenId, attributeNames, uint256Data, bytes32Data, addressData, bytesData);
    }

    function _uploadErc721Lite(
        bytes32 erc721TokenName,
        address owner,
        uint256 tokenId
    ) internal {
        _uploadErc721(
            erc721TokenName,
            owner,
            tokenId,
            new bytes32[](0),
            new uint256[](0),
            new bytes32[](0),
            new address[](0),
            new bytes[](0)
        );
    }
    //map(if token not exists) + sync + withdraw
    function _uploadErc721(
        bytes32 erc721TokenName,
        address owner,
        uint256 tokenId,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Data,
        bytes32[] memory bytes32Data,
        address[] memory addressData,
        bytes[] memory bytesData
    ) internal {
        address erc721Token = ns().getSingleSafe(erc721TokenName);

        require(tokenId != 0, "_uploadErc721, tokenId should never be zero");

        if (!HERC721IMInterface(erc721Token).exists(tokenId)) {
            //map it
            _mapErc721Lite(erc721TokenName, tokenId);
        }

        require(HERC721IMInterface(erc721Token).ownerOf(tokenId) == address(this), "_uploadErc721, tokenId does not belong to AssetVault");

        _withdrawErc721(
            erc721TokenName,
            owner,
            tokenId,
            attributeNames,
            uint256Data,
            bytes32Data,
            addressData,
            bytesData
        );
    }

    function _uploadErc721MoltenLite(
        bytes32 erc721TokenName,
        uint256 tokenId
    ) internal {
        _uploadErc721Molten(
            erc721TokenName,
            tokenId,
            new bytes32[](0),
            new uint256[](0),
            new bytes32[](0),
            new address[](0),
            new bytes[](0)
        );
    }
    //map(if token not exists) + sync + burn in vault
    function _uploadErc721Molten(
        bytes32 erc721TokenName,
        uint256 tokenId,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Data,
        bytes32[] memory bytes32Data,
        address[] memory addressData,
        bytes[] memory bytesData
    ) internal {
        address erc721Token = ns().getSingleSafe(erc721TokenName);

        require(tokenId != 0, "_uploadErc721Burned, tokenId should never be zero");

        if (!HERC721IMInterface(erc721Token).exists(tokenId)) {
            //map it
            _mapErc721Lite(erc721TokenName, tokenId);
        }

        require(HERC721IMInterface(erc721Token).ownerOf(tokenId) == address(this), "_uploadErc721Burned, tokenId does not belong to AssetVault");

        _setAttributeErc721_(
            erc721Token,
            tokenId,
            attributeNames,
            uint256Data,
            bytes32Data,
            addressData,
            bytesData
        );

        //melt
        HERC721IMInterface(erc721Token).burnSudo(tokenId);
        emit MeltErc721(erc721Token, tokenId);
    }

    //never call this function outside
    function _setAttributeErc721_(
        address erc721Token,
        uint256 tokenId,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Data,
        bytes32[] memory bytes32Data,
        address[] memory addressData,
        bytes[] memory bytesData
    ) private {

        //short cut
        if (attributeNames.length == 0) {
            return;
        }

        require(attributeNames.length == (uint256Data.length + bytes32Data.length + addressData.length + bytesData.length), "param length");

        require(tokenId != 0, "_syncErc721, tokenId should never be 0");

        require(HERC721IMInterface(erc721Token).exists(tokenId), "_syncErc721, tokenId should exist");

        uint256 attributeNameIndex = 0;
        for (uint256 i = 0; i < uint256Data.length; i++) {
            HERC721IMInterface(erc721Token).setUint256Attribute(attributeNames[attributeNameIndex], tokenId, uint256Data[i]);
            attributeNameIndex = attributeNameIndex.add(1);
        }

        for (uint256 i = 0; i < bytes32Data.length; i++) {
            HERC721IMInterface(erc721Token).setBytes32Attribute(attributeNames[attributeNameIndex], tokenId, bytes32Data[i]);
            attributeNameIndex = attributeNameIndex.add(1);
        }

        for (uint256 i = 0; i < addressData.length; i++) {
            HERC721IMInterface(erc721Token).setAddressAttribute(attributeNames[attributeNameIndex], tokenId, addressData[i]);
            attributeNameIndex = attributeNameIndex.add(1);
        }

        for (uint256 i = 0; i < bytesData.length; i++) {
            HERC721IMInterface(erc721Token).setBytesAttribute(attributeNames[attributeNameIndex], tokenId, bytesData[i]);
            attributeNameIndex = attributeNameIndex.add(1);
        }
    }

    //=================================getter setter=========================================================
    function _setErc20Strategy(bool withdraw, bytes32 erc20TokenName, uint256 strategy) internal {

        address erc20Token = ns().getSingleSafe(erc20TokenName);

        if (!withdraw) {
            require(AssetVaultType.ERC20_DEPOSIT_STRATEGY_UNKNOWN < strategy && strategy <= AssetVaultType.ERC20_DEPOSIT_STRATEGY_STORE, "deposit strategy invalid");
            _erc20DepositStrategy[erc20Token] = strategy;
        } else {
            require(AssetVaultType.ERC20_WITHDRAW_STRATEGY_UNKNOWN < strategy && strategy <= AssetVaultType.ERC20_WITHDRAW_STRATEGY_STORE_REVERT, "withdraw strategy invalid");
            _erc20WithdrawStrategy[erc20Token] = strategy;
        }
    }

    function _setErc20StrategyAddress(bool withdraw, bytes32 erc20TokenName, address strategyAddress) internal {

        address erc20Token = ns().getSingleSafe(erc20TokenName);

        if (!withdraw) {
            _erc20DepositStrategyAddress[erc20Token] = strategyAddress;
        } else {
            _erc20WithdrawStrategyAddress[erc20Token] = strategyAddress;
        }
    }

    function _setErc1155Strategy(bool withdraw, bytes32 erc1155TokenName, uint256 strategy) internal {

        address erc1155Token = ns().getSingleSafe(erc1155TokenName);

        if (!withdraw) {
            require(AssetVaultType.ERC1155_DEPOSIT_STRATEGY_UNKNOWN < strategy && strategy <= AssetVaultType.ERC1155_DEPOSIT_STRATEGY_STORE, "deposit strategy invalid");
            _erc1155DepositStrategy[erc1155Token] = strategy;
        } else {
            require(AssetVaultType.ERC1155_WITHDRAW_STRATEGY_UNKNOWN < strategy && strategy <= AssetVaultType.ERC1155_WITHDRAW_STRATEGY_STORE_REVERT, "withdraw strategy invalid");
            _erc1155WithdrawStrategy[erc1155Token] = strategy;
        }
    }

    function _setErc1155StrategyAddress(bool withdraw, bytes32 erc1155TokenName, address strategyAddress) internal {

        address erc1155Token = ns().getSingleSafe(erc1155TokenName);

        if (!withdraw) {
            _erc1155DepositStrategyAddress[erc1155Token] = strategyAddress;
        } else {
            _erc1155WithdrawStrategyAddress[erc1155Token] = strategyAddress;
        }
    }

    function _getErc20Strategy(bool withdraw, bytes32 erc20TokenName) view internal returns (uint256 strategy){

        address erc20Token = ns().getSingleSafe(erc20TokenName);

        if (!withdraw) {
            return _erc20DepositStrategy[erc20Token];
        } else {
            return _erc20WithdrawStrategy[erc20Token];
        }
    }

    function _getErc20StrategyAddress(bool withdraw, bytes32 erc20TokenName) view internal returns (address strategyAddress){

        address erc20Token = ns().getSingleSafe(erc20TokenName);

        if (!withdraw) {
            return _erc20DepositStrategyAddress[erc20Token];
        } else {
            return _erc20WithdrawStrategyAddress[erc20Token];
        }
    }

    function _getErc1155Strategy(bool withdraw, bytes32 erc1155TokenName) view internal returns (uint256 strategy){

        address erc1155Token = ns().getSingleSafe(erc1155TokenName);

        if (!withdraw) {
            return _erc1155DepositStrategy[erc1155Token];
        } else {
            return _erc1155WithdrawStrategy[erc1155Token];
        }
    }

    function _setErc1155StrategyAddress(bool withdraw, bytes32 erc1155TokenName) view internal returns (address strategyAddress) {

        address erc1155Token = ns().getSingleSafe(erc1155TokenName);

        if (!withdraw) {
            return _erc1155DepositStrategyAddress[erc1155Token];
        } else {
            return _erc1155WithdrawStrategyAddress[erc1155Token];
        }
    }
}
